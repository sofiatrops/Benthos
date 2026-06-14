using Bep.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Reporting.Infrastructure.Persistence.Configurations;

internal sealed class InformeConfiguration : IEntityTypeConfiguration<Informe>
{
    public void Configure(EntityTypeBuilder<Informe> builder)
    {
        builder.ToTable("informe");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(i => i.Titulo).HasMaxLength(300).IsRequired();
        builder.Property(i => i.TipoEstudio).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(i => i.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(i => i.CampanaId).HasColumnName("campana_id");
        builder.Property(i => i.CentroId).HasColumnName("centro_id");
        builder.Property(i => i.AutorSubjectId).HasColumnName("autor_subject_id").HasMaxLength(200).IsRequired();
        builder.Property(i => i.CreadoUtc).IsRequired();
        builder.Property(i => i.FechaAprobacionUtc);
        builder.Property(i => i.VersionVigenteNumero).HasColumnName("version_vigente_numero").IsRequired();

        builder.OwnsOne(i => i.Periodo, periodo =>
        {
            periodo.Property(p => p.Desde).HasColumnName("periodo_desde").IsRequired();
            periodo.Property(p => p.Hasta).HasColumnName("periodo_hasta").IsRequired();
        });
        builder.Navigation(i => i.Periodo).IsRequired();

        builder.OwnsMany(i => i.Versiones, versiones =>
        {
            versiones.ToTable("version_informe");
            versiones.WithOwner().HasForeignKey("InformeId");
            versiones.HasKey(v => v.Id);
            versiones.Property(v => v.Id).ValueGeneratedNever();
            versiones.Property(v => v.Numero).IsRequired();
            versiones.Property(v => v.ObjectKey).HasColumnName("object_key").HasMaxLength(500).IsRequired();
            versiones.Property(v => v.FechaCargaUtc).IsRequired();
            versiones.Property(v => v.CargadoPorSubjectId).HasColumnName("cargado_por_subject_id").HasMaxLength(200);
        });
        builder.Navigation(i => i.Versiones).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(i => i.Comentarios, comentarios =>
        {
            comentarios.ToTable("comentario_interno");
            comentarios.WithOwner().HasForeignKey("InformeId");
            comentarios.HasKey(c => c.Id);
            comentarios.Property(c => c.Id).ValueGeneratedNever();
            comentarios.Property(c => c.AutorSubjectId).HasColumnName("autor_subject_id").HasMaxLength(200).IsRequired();
            comentarios.Property(c => c.Texto).HasMaxLength(4000).IsRequired();
            comentarios.Property(c => c.FechaUtc).IsRequired();
        });
        builder.Navigation(i => i.Comentarios).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(i => i.Anexos, anexos =>
        {
            anexos.ToTable("anexo");
            anexos.WithOwner().HasForeignKey("InformeId");
            anexos.HasKey(a => a.Id);
            anexos.Property(a => a.Id).ValueGeneratedNever();
            anexos.Property(a => a.ObjectKey).HasColumnName("object_key").HasMaxLength(500).IsRequired();
            anexos.Property(a => a.Descripcion).HasMaxLength(500);
            anexos.Property(a => a.FechaUtc).IsRequired();
        });
        builder.Navigation(i => i.Anexos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => i.Estado);
        builder.HasIndex(i => i.CampanaId);
    }
}
