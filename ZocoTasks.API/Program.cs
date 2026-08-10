using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ZocoTasks.API.Middleware;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Business.Services;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Data;
using ZocoTasks.Infrastructure.Repositories;
using ZocoTasks.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Conexion a la base (PostgreSQL en Neon)
// ---------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("ZocoDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    // Falla al arrancar y no en el primer request: un error de configuracion
    // tiene que ser evidente de inmediato.
    throw new InvalidOperationException(
        "Falta la cadena de conexion 'ZocoDb'. Definirla en user-secrets " +
        "o en la variable de entorno ConnectionStrings__ZocoDb. Ver .env.example.");
}

builder.Services.AddDbContext<ZocoDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        {
            // Neon suspende el compute sin trafico; el reintento evita que ese
            // arranque en frio se vea como un error.
            npgsql.EnableRetryOnFailure();
            npgsql.MigrationsHistoryTable("__ef_migrations_history");
        })
        // snake_case en la base, PascalCase en C#.
        .UseSnakeCaseNamingConvention());

// ---------------------------------------------------------------
// Repositorios y servicios (Scoped: una instancia por request)
// ---------------------------------------------------------------
builder.Services.AddScoped<IComercioRepository, ComercioRepository>();
builder.Services.AddScoped<IInteraccionRepository, InteraccionRepository>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<IRubroRepository, RubroRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

builder.Services.AddScoped<IComercioService, ComercioService>();
builder.Services.AddScoped<IInteraccionService, InteraccionService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IRubroService, RubroService>();
builder.Services.AddScoped<IAnalisisService, AnalisisService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Proveedor de IA para "Analizar oportunidad".
// El timeout es explicito: sin el, HttpClient espera 100 segundos por defecto y
// el usuario se queda mirando un boton colgado. Si Gemini no contesto en 30
// segundos, no va a contestar.
builder.Services.AddHttpClient<IProveedorAnalisisIA, GeminiAnalisisProvider>(cliente =>
{
    cliente.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<GlobalExceptionHandler>();

// Registra por reflexion todos los validadores de FluentValidation.
builder.Services.AddValidatorsFromAssemblyContaining<ComercioService>();

// ---------------------------------------------------------------
// API
// ---------------------------------------------------------------
builder.Services.AddControllers(opciones =>
    {
        // Un cuerpo roto tiene que salir como 400 y no como 500. Ver el
        // comentario del filtro: es la contracara de SuppressModelStateInvalidFilter.
        opciones.Filters.Add<CuerpoRequeridoFilter>();
    })
    .AddJsonOptions(opciones =>
    {
        // Los enums viajan como texto ("Documentacion"), no como numero.
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// La validacion la hacen los servicios con FluentValidation. Se apaga la
// respuesta automatica de MVC para que no compitan dos formatos de error.
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddOpenApi();

// ---------------------------------------------------------------
// Autenticacion JWT
// ---------------------------------------------------------------
var jwtClave = builder.Configuration["Jwt:Clave"];

if (string.IsNullOrWhiteSpace(jwtClave))
{
    throw new InvalidOperationException(
        "Falta 'Jwt:Clave'. Definirla en user-secrets o en la variable de " +
        "entorno Jwt__Clave. Ver .env.example.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            // Emisor y audiencia se desactivan a proposito: son utiles cuando
            // varios sistemas comparten tokens, y aca hay uno solo. Lo que si
            // importa es que el token no este vencido y que la firma sea valida.
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtClave))
        };
    });

builder.Services.AddAuthorization();

// Rate limiting en el login: 2 intentos por minuto por IP. Sin esto, alguien
// puede probar contraseñas a fuerza bruta sin ningun freno.
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opciones.AddPolicy("login", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(politica =>
        politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
            // Sin esto el navegador no deja leer el ETag, y sin el ETag el
            // front no puede mandar If-Match.
            .WithExposedHeaders("ETag"));
});

// Detras del proxy de Render, para que la app sepa que el trafico original
// venia por HTTPS.
builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opciones.KnownIPNetworks.Clear();
    opciones.KnownProxies.Clear();
});

// Render asigna el puerto por variable de entorno. Sin esto la app escucha en
// el puerto equivocado y el deploy queda como "unhealthy".
var puerto = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(puerto))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");
}

var app = builder.Build();

// ---------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------
app.UseForwardedHeaders();

// Primero de todo: cualquier excepcion posterior tiene que salir por aca
// como ProblemDetails.
app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// El orden importa: primero se averigua quien sos (Authentication), despues
// si podes hacer lo que pediste (Authorization). Al reves, User siempre
// llegaria vacio y todo daria 401.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

await SembrarUsuariosAsync(app);

app.Run();

/// <summary>
/// Crea los usuarios de demostracion si la tabla esta vacia.
/// </summary>
/// <remarks>
/// Se hace al arrancar y no con HasData en una migracion porque BCrypt genera
/// una sal distinta cada vez: el hash no es determinista, y EF creeria que el
/// dato cambio en cada build. Ademas evita tener que agregar una migracion.
///
/// Los roles (Admin, Vendedor) ya vienen sembrados desde la migracion inicial.
/// </remarks>
static async Task SembrarUsuariosAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ZocoDbContext>();

    if (await db.Usuarios.AnyAsync())
    {
        return;
    }

    const int RolAdmin = 1;
    const int RolVendedor = 2;

    // Credenciales de demostracion, documentadas en el README.
    var semilla = new (string Email, string Nombre, string Password, int RolId)[]
    {
        ("admin@zoco.test",     "Administrador",    "Admin123!",    RolAdmin),
        ("vendedor1@zoco.test", "Vendedor Uno",     "Vendedor123!", RolVendedor),
        ("vendedor2@zoco.test", "Vendedor Dos",     "Vendedor123!", RolVendedor)
    };

    foreach (var (email, nombre, password, rolId) in semilla)
    {
        var usuario = new Usuario
        {
            Email = email,
            NombreCompleto = nombre,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        usuario.UsuarioRoles.Add(new UsuarioRol { RolId = rolId });
        db.Usuarios.Add(usuario);
    }

    await db.SaveChangesAsync();
}

/// <summary>
/// Necesario para que WebApplicationFactory pueda tomar este ensamblado como
/// punto de entrada en los tests de integracion.
/// </summary>
public partial class Program { }
