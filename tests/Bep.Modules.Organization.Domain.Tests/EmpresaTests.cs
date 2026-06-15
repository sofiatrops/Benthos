using Bep.Modules.Organization.Domain;
using Bep.Modules.Organization.Domain.Events;

namespace Bep.Modules.Organization.Domain.Tests;

public sealed class EmpresaTests
{
    private static Empresa NuevaEmpresa() =>
        Empresa.Registrar("Salmonera Austral S.A.", Rut.Create("76000001-9"), "Acuicultura");

    [Fact]
    public void Registrar_creates_active_empresa_and_raises_event()
    {
        var empresa = NuevaEmpresa();

        Assert.True(empresa.Activa);
        Assert.NotEqual(Guid.Empty, empresa.Id);
        Assert.Contains(empresa.DomainEvents, e => e is EmpresaRegistrada);
    }

    [Fact]
    public void Registrar_rejects_empty_razon_social()
    {
        Assert.Throws<ArgumentException>(() =>
            Empresa.Registrar("  ", Rut.Create("76000001-9"), "Acuicultura"));
    }

    [Fact]
    public void Provisionar_uses_preassigned_identity()
    {
        var id = Guid.NewGuid();

        var empresa = Empresa.Provisionar(id, "Salmonera Austral S.A.", Rut.Create("76000001-9"), "Acuicultura");

        Assert.Equal(id, empresa.Id);
        Assert.True(empresa.Activa);
        Assert.Contains(empresa.DomainEvents, e => e is EmpresaRegistrada);
    }

    [Fact]
    public void Provisionar_rejects_empty_identity()
    {
        Assert.Throws<ArgumentException>(() =>
            Empresa.Provisionar(Guid.Empty, "Salmonera Austral S.A.", Rut.Create("76000001-9"), "Acuicultura"));
    }

    [Fact]
    public void AgregarCentro_links_centro_to_empresa_tenant()
    {
        var empresa = NuevaEmpresa();

        var centro = empresa.AgregarCentro(
            "Centro Quellón", "QLL-01", CoordenadasGps.Create(-43.12, -73.62), "Los Lagos");

        Assert.Equal(empresa.Id, centro.TenantId);
        Assert.Single(empresa.Centros);
    }

    [Fact]
    public void Desactivar_marks_empresa_inactive_without_removing()
    {
        var empresa = NuevaEmpresa();

        empresa.Desactivar();

        Assert.False(empresa.Activa);
    }
}
