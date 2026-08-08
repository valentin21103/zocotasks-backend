using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Entidad).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntidadId).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Accion).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Fecha).HasDefaultValueSql("now()");

        // jsonb: la forma del diccionario de cambios depende de la entidad
        // auditada, asi que no hay un esquema relacional que imponerle.
        builder.Property(a => a.Cambios).HasColumnType("jsonb");

        builder.HasOne(a => a.Usuario)
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .HasConstraintName("fk_audit_log_usuario")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.Entidad, a.EntidadId })
            .HasDatabaseName("ix_audit_log_entidad");

        builder.HasIndex(a => a.Fecha).HasDatabaseName("ix_audit_log_fecha");
    }
}
