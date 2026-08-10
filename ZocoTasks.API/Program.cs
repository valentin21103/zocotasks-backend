using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZocoTasks.API.Middleware;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Business.Services;
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

builder.Services.AddScoped<IComercioService, ComercioService>();
builder.Services.AddScoped<IInteraccionService, InteraccionService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IRubroService, RubroService>();
builder.Services.AddScoped<IAnalisisService, AnalisisService>();

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
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Necesario para que WebApplicationFactory pueda tomar este ensamblado como
/// punto de entrada en los tests de integracion.
/// </summary>
public partial class Program { }
