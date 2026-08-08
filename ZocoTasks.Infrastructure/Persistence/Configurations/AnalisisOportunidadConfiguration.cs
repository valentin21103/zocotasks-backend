using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Persistence.Configurations;

public class AnalisisOportunidadConfiguration : IEntityTypeConfiguration<AnalisisOportunidad>
{
    public void Configure(EntityTypeBuilder<AnalisisOportunidad> builder)
    {
        builder.ToTable("analisis_oportunidad");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.NivelInteres)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(a => a.Resumen).HasColumnType("text").IsRequired();
        builder.Property(a => a.ProximoPaso).HasColumnType("text").IsRequired();

        // jsonb: arrays de solo lectura sobre los que no se consulta de forma
        // relacional. Npgsql serializa List<string> a jsonb de forma nativa.
        builder.Property(a => a.PreguntasSugeridas).HasColumnType("jsonb");
        builder.Property(a => a.DatosFaltantes).HasColumnType("jsonb");

        builder.Property(a => a.ModeloUtilizado).HasMaxLength(100).IsRequired();

        // SHA256 en hexadecimal: largo fijo de 64.
        builder.Property(a => a.HashContexto)
            .HasColumnType("char(64)")
            .IsRequired();

        builder.Property(a => a.FechaGeneracion).HasDefaultValueSql("now()");

        builder.Property(a => a.EsDegradado).HasDefaultValue(false);

        builder.HasOne(a => a.Comercio)
            .WithMany(c => c.Analisis)
            .HasForeignKey(a => a.ComercioId)
            .HasConstraintName("fk_analisis_comercio")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Usuario)
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .HasConstraintName("fk_analisis_usuario")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.ComercioId, a.FechaGeneracion })
            .HasDatabaseName("ix_analisis_comercio_fecha");

        // Clave de cache: se busca por comercio + hash antes de llamar al modelo.
        builder.HasIndex(a => new { a.ComercioId, a.HashContexto })
            .HasDatabaseName("ix_analisis_comercio_hash");
    }
}
