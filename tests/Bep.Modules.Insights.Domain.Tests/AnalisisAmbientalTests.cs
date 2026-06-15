using Bep.Modules.Insights.Domain;
using Bep.Modules.Insights.Domain.Events;

namespace Bep.Modules.Insights.Domain.Tests;

public sealed class AnalisisAmbientalTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Campana = Guid.NewGuid();

    private static AnalisisAmbiental Nuevo() =>
        AnalisisAmbiental.Generar(Tenant, Campana, "Resumen de prueba", "deterministic-v1",
        [
            Hallazgo.Crear("pH", SeveridadHallazgo.Informativo, "Estable alrededor de 7.9."),
        ]);

    [Fact]
    public void Generar_crea_borrador_con_evento_y_modelo()
    {
        var analisis = Nuevo();

        Assert.Equal(EstadoAnalisis.Borrador, analisis.Estado);
        Assert.Equal("deterministic-v1", analisis.Modelo);
        Assert.Single(analisis.Hallazgos);
        Assert.Contains(analisis.DomainEvents, e => e is AnalisisGenerado);
    }

    [Fact]
    public void Generar_exige_resumen_y_modelo()
    {
        Assert.Throws<ArgumentException>(() =>
            AnalisisAmbiental.Generar(Tenant, Campana, "  ", "modelo", []));
        Assert.Throws<ArgumentException>(() =>
            AnalisisAmbiental.Generar(Tenant, Campana, "resumen", "  ", []));
    }

    [Fact]
    public void Validar_marca_validado_con_validador_y_evento()
    {
        var analisis = Nuevo();

        analisis.Validar("revisor-1");

        Assert.Equal(EstadoAnalisis.Validado, analisis.Estado);
        Assert.Equal("revisor-1", analisis.ValidadoPorSubjectId);
        Assert.NotNull(analisis.ValidadoUtc);
        Assert.Contains(analisis.DomainEvents, e => e is AnalisisValidado);
    }

    [Fact]
    public void Descartar_requiere_motivo()
    {
        var analisis = Nuevo();

        Assert.Throws<ArgumentException>(() => analisis.Descartar("  "));
    }

    [Fact]
    public void No_se_puede_validar_un_analisis_descartado()
    {
        var analisis = Nuevo();
        analisis.Descartar("Interpretación incorrecta");

        Assert.Throws<InvalidOperationException>(() => analisis.Validar("revisor-1"));
    }

    [Fact]
    public void No_se_puede_validar_dos_veces()
    {
        var analisis = Nuevo();
        analisis.Validar("revisor-1");

        Assert.Throws<InvalidOperationException>(() => analisis.Validar("revisor-2"));
    }
}
