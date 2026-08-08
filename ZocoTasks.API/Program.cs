using ZocoTasks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Registro de la capa de infraestructura (DbContext, repositorios, servicios
// de proveedor). La API no conoce EF Core mas alla de esta linea.
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Necesario para que <c>WebApplicationFactory</c> pueda tomar este ensamblado
/// como punto de entrada en los tests de integracion.
/// </summary>
public partial class Program { }
