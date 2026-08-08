using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Data;

namespace ZocoTasks.Infrastructure.Data.Configurations;

public class TipoInteraccionConfiguration : IEntityTypeConfiguration<TipoInteraccion>
{
    public void Configure(EntityTypeBuilder<TipoInteraccion> builder)
    {
        builder.ToTable("tipo_interaccion");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnType("smallint")
            .ValueGeneratedNever();

        builder.Property(t => t.Codigo).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Nombre).HasMaxLength(50).IsRequired();

        builder.HasIndex(t => t.Codigo)
            .IsUnique()
            .HasDatabaseName("ux_tipo_interaccion_codigo");

        builder.HasData(CatalogosSeed.TiposInteraccion);
    }
}
