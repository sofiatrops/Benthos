using Bep.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bep.Modules.Organization.Infrastructure.Persistence.Configurations;

internal sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresa");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.RazonSocial).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Rubro).HasMaxLength(200);
        builder.Property(e => e.Activa).IsRequired();
        builder.Property(e => e.CreadaUtc).IsRequired();

        builder.Property(e => e.Rut)
            .HasConversion(rut => rut.Value, value => Rut.Create(value))
            .HasColumnName("rut")
            .HasMaxLength(20)
            .IsRequired();

        // RUT único a nivel global (RF-01-001).
        builder.HasIndex(e => e.Rut).IsUnique();

        builder.HasMany(e => e.Centros)
            .WithOne()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata
            .FindNavigation(nameof(Empresa.Centros))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
