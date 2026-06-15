using Bep.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Laboratory.Infrastructure.Persistence.Configurations;

internal sealed class LoteResultadosConfiguration : IEntityTypeConfiguration<LoteResultados>
{
    public void Configure(EntityTypeBuilder<LoteResultados> builder)
    {
        builder.ToTable("lote_resultados");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(l => l.CampanaId).HasColumnName("campana_id").IsRequired();
        builder.Property(l => l.Laboratorio).HasMaxLength(200).IsRequired();
        builder.Property(l => l.ArchivoObjectKey).HasColumnName("archivo_object_key").HasMaxLength(500).IsRequired();
        builder.Property(l => l.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.RecibidoUtc).HasColumnName("recibido_utc").IsRequired();
        builder.Property(l => l.ValidadoUtc).HasColumnName("validado_utc");
        builder.Property(l => l.MotivoRechazo).HasColumnName("motivo_rechazo").HasMaxLength(1000);

        builder.OwnsMany(l => l.Resultados, resultados =>
        {
            resultados.ToTable("resultado_parametro");
            resultados.WithOwner().HasForeignKey("LoteResultadosId");
            resultados.HasKey(r => r.Id);
            resultados.Property(r => r.Id).ValueGeneratedNever();
            resultados.Property(r => r.CodigoMuestra).HasColumnName("codigo_muestra").HasMaxLength(40).IsRequired();
            resultados.Property(r => r.Parametro).HasMaxLength(120).IsRequired();
            resultados.Property(r => r.Valor).IsRequired();
            resultados.Property(r => r.Unidad).HasMaxLength(40).IsRequired();
            resultados.Property(r => r.Metodo).HasMaxLength(120);
            resultados.HasIndex("LoteResultadosId");
            resultados.HasIndex(r => r.CodigoMuestra);
        });
        builder.Navigation(l => l.Resultados).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => l.CampanaId);
        builder.HasIndex(l => l.Estado);
    }
}
