using Bep.Modules.Sampling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Sampling.Infrastructure.Persistence.Configurations;

internal sealed class MuestraConfiguration : IEntityTypeConfiguration<Muestra>
{
    public void Configure(EntityTypeBuilder<Muestra> builder)
    {
        builder.ToTable("muestra");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(m => m.CampanaId).HasColumnName("campana_id").IsRequired();
        builder.Property(m => m.CentroId).HasColumnName("centro_id").IsRequired();

        builder.Property(m => m.CodigoUnico).HasColumnName("codigo_unico").HasMaxLength(40).IsRequired();
        builder.HasIndex(m => m.CodigoUnico).IsUnique();

        builder.Property(m => m.CodigoQr)
            .HasConversion(qr => qr.Value, value => CodigoQr.Create(value))
            .HasColumnName("codigo_qr")
            .HasMaxLength(60)
            .IsRequired();
        builder.HasIndex(m => m.CodigoQr).IsUnique();

        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.FechaRegistroUtc).IsRequired();

        builder.OwnsOne(m => m.Ubicacion, ubicacion =>
        {
            ubicacion.Property(u => u.Latitud).HasColumnName("latitud").IsRequired();
            ubicacion.Property(u => u.Longitud).HasColumnName("longitud").IsRequired();
            ubicacion.Property(u => u.PrecisionMetros).HasColumnName("precision_metros");
        });
        builder.Navigation(m => m.Ubicacion).IsRequired();

        builder.PrimitiveCollection(m => m.ParametrosSolicitados)
            .HasColumnName("parametros_solicitados")
            .HasField("_parametrosSolicitados")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(m => m.Fotos)
            .HasColumnName("fotos")
            .HasField("_fotos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Historial de eventos (RF-03-006) en tabla propia.
        builder.OwnsMany(m => m.Eventos, eventos =>
        {
            eventos.ToTable("evento_muestra");
            eventos.WithOwner().HasForeignKey("MuestraId");
            eventos.HasKey(e => e.Id);
            eventos.Property(e => e.Id).ValueGeneratedNever();
            eventos.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
            eventos.Property(e => e.FechaUtc).IsRequired();
            eventos.Property(e => e.UsuarioSubjectId).HasMaxLength(200);
            eventos.Property(e => e.Descripcion).HasMaxLength(1000).IsRequired();
        });
        builder.Navigation(m => m.Eventos).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Cadena de custodia (RF-03-007) en tabla propia.
        builder.OwnsMany(m => m.Custodias, custodias =>
        {
            custodias.ToTable("registro_custodia");
            custodias.WithOwner().HasForeignKey("MuestraId");
            custodias.HasKey(c => c.Id);
            custodias.Property(c => c.Id).ValueGeneratedNever();
            custodias.Property(c => c.DeSubjectId).HasMaxLength(200);
            custodias.Property(c => c.ParaSubjectId).HasMaxLength(200).IsRequired();
            custodias.Property(c => c.FechaTransferenciaUtc).IsRequired();
            custodias.Property(c => c.Aceptada).IsRequired();
            custodias.Property(c => c.FechaAceptacionUtc);
        });
        builder.Navigation(m => m.Custodias).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.CampanaId);
    }
}
