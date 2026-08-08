using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Data.Configurations;

public class InteraccionConfiguration : IEntityTypeConfiguration<Interaccion>
{
    public void Configure(EntityTypeBuilder<Interaccion> builder)
    {
        builder.ToTable("interaccion");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Detalle)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(i => i.Fecha).IsRequired();

        builder.Property(i => i.FechaCreacion)
            .HasDefaultValueSql("now()");

        builder.Property(i => i.Tipo)
            .HasColumnName("tipo_id")
            .HasColumnType("smallint")
            .IsRequired();

        // Cascade: las interacciones no tienen sentido sin su comercio. Solo se
        // dispara en un borrado fisico; la baja normal es logica.
        builder.HasOne(i => i.Comercio)
            .WithMany(c => c.Interacciones)
            .HasForeignKey(i => i.ComercioId)
            .HasConstraintName("fk_interaccion_comercio")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.TipoNavegacion)
            .WithMany(t => t.Interacciones)
            .HasForeignKey(i => i.Tipo)
            .HasConstraintName("fk_interaccion_tipo")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Usuario)
            .WithMany()
            .HasForeignKey(i => i.UsuarioId)
            .HasConstraintName("fk_interaccion_usuario")
            .OnDelete(DeleteBehavior.SetNull);

        // Compuesto: el acceso natural es "las interacciones de este comercio,
        // de la mas reciente a la mas vieja".
        builder.HasIndex(i => new { i.ComercioId, i.Fecha })
            .HasDatabaseName("ix_interaccion_comercio_fecha");
    }
}
