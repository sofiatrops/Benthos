using Bep.Modules.Campaign.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Campaign.Infrastructure.Persistence.Configurations;

internal sealed class CampaniaConfiguration : IEntityTypeConfiguration<Campania>
{
    public void Configure(EntityTypeBuilder<Campania> builder)
    {
        builder.ToTable("campania");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.Nombre).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Descripcion).HasMaxLength(2000);

        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();

        // Periodo como tipo poseído (dos columnas de fecha).
        builder.OwnsOne(c => c.Periodo, periodo =>
        {
            periodo.Property(p => p.Inicio).HasColumnName("fecha_inicio").IsRequired();
            periodo.Property(p => p.Fin).HasColumnName("fecha_fin").IsRequired();
        });
        builder.Navigation(c => c.Periodo).IsRequired();

        // Centros asociados como colección primitiva (jsonb).
        builder.PrimitiveCollection(c => c.CentroIds)
            .HasColumnName("centro_ids")
            .HasField("_centroIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Responsables como colección de objetos de valor serializada a JSON.
        builder.OwnsMany(c => c.Responsables, responsables =>
        {
            responsables.ToJson("responsables");
            responsables.Property(r => r.SubjectId);
            responsables.Property(r => r.Rol);
        });
        builder.Navigation(c => c.Responsables).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Estado);
    }
}
