using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Infrastructure.Data;
using ZocoTasks.Infrastructure.Repositories;

namespace ZocoTasks.Infrastructure;

/// <summary>
/// Punto unico de registro de la capa de infraestructura. La API referencia a
/// Infrastructure solo por esto: es la unica concesion a la regla de que los
/// controllers hablan nada mas que con Business.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Nombre de la cadena de conexion en configuracion.</summary>
    public const string ConnectionStringName = "ZocoDb";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Falla en el arranque y no cuando entra el primer request: un error
            // de configuracion tiene que ser evidente de inmediato.
            throw new InvalidOperationException(
                $"Falta la cadena de conexion '{ConnectionStringName}'. " +
                $"Definirla via la variable de entorno " +
                $"ConnectionStrings__{ConnectionStringName} o en user-secrets. " +
                "Ver .env.example.");
        }

        services.AddDbContext<ZocoDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    // Neon es serverless: la primera conexion despues de un
                    // periodo de inactividad despierta el endpoint y puede
                    // demorar. El reintento evita que ese arranque en frio se
                    // vea como un error de la aplicacion.
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                    npgsql.MigrationsHistoryTable("__ef_migrations_history");
                })
                // snake_case en la base, PascalCase en C#. Sin esto Postgres
                // exigiria comillas dobles en cada identificador.
                .UseSnakeCaseNamingConvention());

        // Repositorios. Scoped, igual que el DbContext: comparten la misma
        // unidad de trabajo dentro de un request.
        services.AddScoped<IComercioRepository, ComercioRepository>();
        services.AddScoped<IInteraccionRepository, InteraccionRepository>();
        services.AddScoped<ICatalogoRepository, CatalogoRepository>();

        return services;
    }
}
