using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Persistence.Seed;

namespace ZocoTasks.Infrastructure.Persistence.Configurations;

public class EstadoComercioConfiguration : IEntityTypeConfiguration<EstadoComercio>
{
    public void Configure(EntityTypeBuilder<EstadoComercio> builder)
    {
        builder.ToTable("estado_comercio");

        builder.HasKey(e => e.Id);

        // Los ids son los del enum: no los genera la base.
        builder.Property(e => e.Id)
            .HasColumnType("smallint")
            .ValueGeneratedNever();

        builder.Property(e => e.Codigo).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Nombre).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Orden).HasColumnType("smallint").IsRequired();
        builder.Property(e => e.EsFinal).IsRequired();

        builder.HasIndex(e => e.Codigo)
            .IsUnique()
            .HasDatabaseName("ux_estado_comercio_codigo");

        builder.HasData(CatalogosSeed.Estados);
    }
}
