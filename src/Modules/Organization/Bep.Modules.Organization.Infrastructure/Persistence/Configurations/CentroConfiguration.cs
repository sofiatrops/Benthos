using Bep.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Organization.Infrastructure.Persistence.Configurations;

internal sealed class CentroConfiguration : IEntityTypeConfiguration<Centro>
{
    public void Configure(EntityTypeBuilder<Centro> builder)
    {
        builder.ToTable("centro");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.Nombre).HasMaxLength(300).IsRequired();
        builder.Property(c => c.CodigoInterno).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Region).HasMaxLength(150);
        builder.Property(c => c.Activo).IsRequired();

        // Coordenadas como tipo poseído (lat/lon). Evolución: proyectar a punto
        // PostGIS (geometry) para consultas espaciales (SRS 2.8.3).
        builder.OwnsOne(c => c.Coordenadas, nav =>
        {
            nav.Property(p => p.Latitud).HasColumnName("latitud").IsRequired();
            nav.Property(p => p.Longitud).HasColumnName("longitud").IsRequired();
        });

        // Código interno único dentro de cada empresa.
        builder.HasIndex(c => new { c.TenantId, c.CodigoInterno }).IsUnique();
    }
}
