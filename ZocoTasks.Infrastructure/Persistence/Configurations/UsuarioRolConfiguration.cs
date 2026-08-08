using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Persistence.Configurations;

public class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.ToTable("usuario_rol");

        // PK compuesta: un usuario no puede tener dos veces el mismo rol.
        builder.HasKey(ur => new { ur.UsuarioId, ur.RolId });

        builder.HasOne(ur => ur.Usuario)
            .WithMany(u => u.UsuarioRoles)
            .HasForeignKey(ur => ur.UsuarioId)
            .HasConstraintName("fk_usuario_rol_usuario")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Rol)
            .WithMany(r => r.UsuarioRoles)
            .HasForeignKey(ur => ur.RolId)
            .HasConstraintName("fk_usuario_rol_rol")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
