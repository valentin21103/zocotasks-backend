using Microsoft.EntityFrameworkCore;
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
}
