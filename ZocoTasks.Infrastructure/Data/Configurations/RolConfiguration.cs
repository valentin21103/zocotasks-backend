using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Data;

namespace ZocoTasks.Infrastructure.Data.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Nombre).HasMaxLength(50).IsRequired();

        builder.HasIndex(r => r.Nombre)
            .IsUnique()
            .HasDatabaseName("ux_rol_nombre");

        builder.HasData(CatalogosSeed.Roles);
    }
}
