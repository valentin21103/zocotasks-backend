using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Persistence.Seed;

namespace ZocoTasks.Infrastructure.Persistence.Configurations;

public class RubroConfiguration : IEntityTypeConfiguration<Rubro>
{
    public void Configure(EntityTypeBuilder<Rubro> builder)
    {
        builder.ToTable("rubro");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Activo).HasDefaultValue(true);

        builder.HasIndex(r => r.Nombre)
            .IsUnique()
            .HasDatabaseName("ux_rubro_nombre");

        builder.HasData(CatalogosSeed.Rubros);
    }
}
