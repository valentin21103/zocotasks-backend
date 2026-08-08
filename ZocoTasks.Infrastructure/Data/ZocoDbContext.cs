using Microsoft.EntityFrameworkCore;
using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Data;

/// <summary>
/// Contexto de EF Core. La convencion snake_case no se declara aca sino en
/// <c>UseSnakeCaseNamingConvention()</c> al registrar el proveedor, para que
/// aplique tambien a las columnas que genera la propia migracion.
/// </summary>
public class ZocoDbContext : DbContext
{
    public ZocoDbContext(DbContextOptions<ZocoDbContext> options) : base(options) { }

    public DbSet<Comercio> Comercios => Set<Comercio>();
    public DbSet<Interaccion> Interacciones => Set<Interaccion>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();

    public DbSet<Rubro> Rubros => Set<Rubro>();
    public DbSet<EstadoComercio> EstadosComercio => Set<EstadoComercio>();
    public DbSet<TipoInteraccion> TiposInteraccion => Set<TipoInteraccion>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // citext se usa para los emails: la comparacion case-insensitive la
        // resuelve el tipo de Postgres, no el codigo de aplicacion.
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZocoDbContext).Assembly);
    }

    /// <summary>
    /// Mantiene las fechas de alta y modificacion de toda entidad
    /// <see cref="IAuditable"/>.
    /// </summary>
    /// <remarks>
    /// Se hace aca y no en cada servicio para que sea imposible olvidarse:
    /// un solo camino que no las setee dejaria datos inconsistentes, y ese es
    /// el tipo de bug que no se nota hasta que alguien ordena por fecha.
    /// Se usa siempre UTC porque las columnas son <c>timestamptz</c>.
    /// </remarks>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;

        foreach (var entrada in ChangeTracker.Entries<IAuditable>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.FechaCreacion = ahora;
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Entity.FechaActualizacion = ahora;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
