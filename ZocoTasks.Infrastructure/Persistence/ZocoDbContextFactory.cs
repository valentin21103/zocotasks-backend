using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZocoTasks.Infrastructure.Persistence;

/// <summary>
/// Factory que usan las herramientas de linea de comandos de EF Core
/// (<c>dotnet ef migrations add</c>, <c>database update</c>).
/// </summary>
/// <remarks>
/// Existe para desacoplar el diseño de migraciones del arranque de la API: sin
/// esto, generar una migracion obligaria a levantar todo el host y por lo tanto
/// a tener la cadena de conexion real disponible.
///
/// Para <c>migrations add</c> alcanza con una cadena sintacticamente valida,
/// porque EF solo necesita construir el modelo; recien <c>database update</c>
/// se conecta de verdad. Por eso hay un valor de reserva: permite generar
/// migraciones sin credenciales y sin red.
/// </remarks>
public class ZocoDbContextFactory : IDesignTimeDbContextFactory<ZocoDbContext>
{
    private const string VariableEntorno = "ConnectionStrings__ZocoDb";

    /// <summary>
    /// Placeholder solo para construir el modelo. No apunta a ninguna base real:
    /// si se filtrara a un comando que intenta conectarse, el fallo es inmediato
    /// y evidente en lugar de silencioso.
    /// </summary>
    private const string CadenaDeDiseño =
        "Host=localhost;Port=5432;Database=zocotasks_design_time;Username=postgres;Password=postgres";

    public ZocoDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(VariableEntorno) ?? CadenaDeDiseño;

        var options = new DbContextOptionsBuilder<ZocoDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ZocoDbContext(options);
    }
}
