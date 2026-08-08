using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");

        builder.HasKey(u => u.Id);

        // citext: el indice unico de abajo ya rechaza el mismo mail escrito con
        // otras mayusculas, sin normalizar en el codigo.
        builder.Property(u => u.Email)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
        builder.Property(u => u.NombreCompleto).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Activo).HasDefaultValue(true);
        builder.Property(u => u.FechaCreacion).HasDefaultValueSql("now()");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_usuario_email");
    }
}
