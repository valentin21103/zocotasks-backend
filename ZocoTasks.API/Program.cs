using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ZocoTasks.API.Middleware;
using ZocoTasks.Business;
using ZocoTasks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------
// Servicios
// -----------------------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        // Los enums viajan como texto ("Documentacion") en lugar de como
        // numero. Hace la API legible y evita que el front tenga que mantener
        // su propia tabla de equivalencias.
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// La validacion la hacen los servicios con FluentValidation, que devuelve el
// detalle campo por campo. Se apaga la respuesta automatica de MVC para que no
// compitan dos formatos de error distintos.
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBusiness();

builder.Services.AddScoped<GlobalExceptionHandler>();

// CORS: el frontend vive en otro repositorio y por lo tanto en otro origen.
// Los origenes permitidos se configuran por variable de entorno; en desarrollo
// se admite cualquiera para no frenar el trabajo.
var origenesPermitidos = builder.Configuration
    .GetSection("Cors:Origenes").Get<string[]>() ?? [];

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(politica =>
    {
        if (origenesPermitidos.Length > 0)
        {
            politica.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod()
                // Sin esto el navegador no deja que el front lea el ETag, y sin
                // el ETag no puede mandar If-Match.
                .WithExposedHeaders("ETag");
        }
        else
        {
            politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
                .WithExposedHeaders("ETag");
        }
    });
});

var app = builder.Build();

// -----------------------------------------------------------------
// Pipeline
// -----------------------------------------------------------------

// Primero de todo: cualquier excepcion que ocurra mas adelante tiene que pasar
// por aca para salir como ProblemDetails.
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
/// Necesario para que <c>WebApplicationFactory</c> pueda tomar este ensamblado
/// como punto de entrada en los tests de integracion.
/// </summary>
public partial class Program { }
