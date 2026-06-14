using Bep.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Audit.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.OccurredOnUtc).IsRequired();
        builder.Property(a => a.EventType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.TenantId);
        builder.Property(a => a.ActorSubjectId).HasMaxLength(200);
        builder.Property(a => a.PayloadJson).HasColumnType("jsonb").IsRequired();

        // Índices para la vista de auditoría filtrable (RF-08-006).
        builder.HasIndex(a => a.OccurredOnUtc);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.EventType);
    }
}
