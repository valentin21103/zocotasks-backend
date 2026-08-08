using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Persistence.Configurations;

public class HistorialEstadoConfiguration : IEntityTypeConfiguration<HistorialEstado>
{
    public void Configure(EntityTypeBuilder<HistorialEstado> builder)
    {
        builder.ToTable("historial_estado");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.EstadoAnterior)
            .HasColumnName("estado_anterior_id")
            .HasColumnType("smallint");

        builder.Property(h => h.EstadoNuevo)
            .HasColumnName("estado_nuevo_id")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(h => h.Fecha).IsRequired();

        builder.Property(h => h.Motivo).HasMaxLength(500);

        builder.HasOne(h => h.Comercio)
            .WithMany(c => c.Historial)
            .HasForeignKey(h => h.ComercioId)
            .HasConstraintName("fk_historial_comercio")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EstadoComercio>()
            .WithMany()
            .HasForeignKey(h => h.EstadoAnterior)
            .HasConstraintName("fk_historial_estado_anterior")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<EstadoComercio>()
            .WithMany()
            .HasForeignKey(h => h.EstadoNuevo)
            .HasConstraintName("fk_historial_estado_nuevo")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .HasConstraintName("fk_historial_usuario")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => new { h.ComercioId, h.Fecha })
            .HasDatabaseName("ix_historial_comercio_fecha");
    }
}
