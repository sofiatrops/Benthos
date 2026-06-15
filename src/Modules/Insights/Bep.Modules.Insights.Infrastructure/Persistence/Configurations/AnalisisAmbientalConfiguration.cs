using Bep.Modules.Insights.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Insights.Infrastructure.Persistence.Configurations;

internal sealed class AnalisisAmbientalConfiguration : IEntityTypeConfiguration<AnalisisAmbiental>
{
    public void Configure(EntityTypeBuilder<AnalisisAmbiental> builder)
    {
        builder.ToTable("analisis_ambiental");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(a => a.CampanaId).HasColumnName("campana_id").IsRequired();
        builder.Property(a => a.Resumen).HasMaxLength(8000).IsRequired();
        builder.Property(a => a.Modelo).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.GeneradoUtc).HasColumnName("generado_utc").IsRequired();
        builder.Property(a => a.ValidadoUtc).HasColumnName("validado_utc");
        builder.Property(a => a.ValidadoPorSubjectId).HasColumnName("validado_por_subject_id").HasMaxLength(200);
        builder.Property(a => a.MotivoDescarte).HasColumnName("motivo_descarte").HasMaxLength(1000);

        builder.OwnsMany(a => a.Hallazgos, hallazgos =>
        {
            hallazgos.ToTable("hallazgo");
            hallazgos.WithOwner().HasForeignKey("AnalisisAmbientalId");
            hallazgos.HasKey(h => h.Id);
            hallazgos.Property(h => h.Id).ValueGeneratedNever();
            hallazgos.Property(h => h.Parametro).HasMaxLength(120).IsRequired();
            hallazgos.Property(h => h.Severidad).HasConversion<string>().HasMaxLength(20).IsRequired();
            hallazgos.Property(h => h.Detalle).HasMaxLength(2000).IsRequired();
            hallazgos.HasIndex("AnalisisAmbientalId");
        });
        builder.Navigation(a => a.Hallazgos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.CampanaId);
        builder.HasIndex(a => a.Estado);
    }
}
