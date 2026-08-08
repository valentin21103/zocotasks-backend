using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Data.Configurations;

public class ComercioConfiguration : IEntityTypeConfiguration<Comercio>
{
    /// <summary>
    /// Nombre de la shadow property del vector de busqueda. Se expone para que
    /// el repositorio pueda referenciarla con <c>EF.Property</c> sin repetir
    /// el literal.
    /// </summary>
    public const string SearchVectorProperty = "SearchVector";

    public void Configure(EntityTypeBuilder<Comercio> builder)
    {
        builder.ToTable("comercio");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NombreComercial)
            .HasMaxLength(150)
            .IsRequired();

        // char(11): el CUIT tiene largo fijo y se guarda sin guiones.
        builder.Property(c => c.Cuit)
            .HasColumnType("char(11)")
            .IsRequired();

        builder.Property(c => c.NombreContacto)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(c => c.Telefono)
            .HasMaxLength(30);

        // citext en lugar de varchar: el indice y las comparaciones ignoran
        // mayusculas a nivel de tipo, sin normalizar en cada insert.
        builder.Property(c => c.Email)
            .HasColumnType("citext");

        builder.Property(c => c.Notas)
            .HasColumnType("text");

        builder.Property(c => c.FechaCreacion)
            .HasDefaultValueSql("now()");

        // --- Concurrencia optimista -------------------------------------
        // xmin es una columna de sistema de PostgreSQL que cambia sola en cada
        // UPDATE. Mapearla evita mantener una columna de version propia y hace
        // imposible olvidarse de incrementarla.
        builder.Property(c => c.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // --- Relaciones ---------------------------------------------------
        builder.HasOne(c => c.EstadoNavegacion)
            .WithMany(e => e.Comercios)
            .HasForeignKey(c => c.Estado)
            .HasConstraintName("fk_comercio_estado")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Estado)
            .HasColumnName("estado_id")
            .HasColumnType("smallint")
            .IsRequired();

        builder.HasOne(c => c.Rubro)
            .WithMany(r => r.Comercios)
            .HasForeignKey(c => c.RubroId)
            .HasConstraintName("fk_comercio_rubro")
            .OnDelete(DeleteBehavior.Restrict);

        // El comercio sobrevive a la baja del vendedor que lo tenia asignado.
        builder.HasOne(c => c.UsuarioAsignado)
            .WithMany(u => u.ComerciosAsignados)
            .HasForeignKey(c => c.UsuarioAsignadoId)
            .HasConstraintName("fk_comercio_usuario_asignado")
            .OnDelete(DeleteBehavior.SetNull);

        // --- Busqueda full text -------------------------------------------
        // Shadow property: NpgsqlTsVector es un tipo del proveedor y Domain no
        // referencia paquetes. Columna generada por la base (STORED), asi que
        // se mantiene sola en cada insert y update.
        builder.Property<NpgsqlTsVector>(SearchVectorProperty)
            .HasColumnName("search_vector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                """
                to_tsvector('spanish',
                    coalesce(nombre_comercial, '') || ' ' ||
                    coalesce(nombre_contacto, '')  || ' ' ||
                    coalesce(notas, ''))
                """,
                stored: true);

        // --- Indices --------------------------------------------------------
        builder.HasIndex(c => c.Cuit)
            .IsUnique()
            .HasDatabaseName("ux_comercio_cuit");

        builder.HasIndex(c => c.Estado).HasDatabaseName("ix_comercio_estado");
        builder.HasIndex(c => c.RubroId).HasDatabaseName("ix_comercio_rubro");
        builder.HasIndex(c => c.FechaCreacion).HasDatabaseName("ix_comercio_fecha_creacion");

        builder.HasIndex(SearchVectorProperty)
            .HasDatabaseName("ix_comercio_search_vector")
            .HasMethod("GIN");

        // --- Baja logica ----------------------------------------------------
        // Filtro global: los comercios eliminados desaparecen de toda consulta
        // salvo que se pida explicitamente con IgnoreQueryFilters().
        builder.HasQueryFilter(c => c.FechaEliminacion == null);
    }
}
