using Bep.Modules.Laboratory.Domain;
using Bep.Modules.Laboratory.Domain.Events;

namespace Bep.Modules.Laboratory.Domain.Tests;

public sealed class LoteResultadosTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Campana = Guid.NewGuid();

    private static LoteResultados NuevoLote() =>
        LoteResultados.Recibir(Tenant, Campana, "Laboratorio Austral", "tenant/lab/file.csv",
        [
            ResultadoParametro.Crear("MTR-20260601-ABCDE12345", "Oxígeno disuelto", 8.4, "mg/L", "SM 4500-O"),
            ResultadoParametro.Crear("MTR-20260601-ABCDE12345", "pH", 7.9, "pH", null),
        ]);

    [Fact]
    public void Recibir_crea_lote_en_estado_recibido_con_evento()
    {
        var lote = NuevoLote();

        Assert.Equal(EstadoLote.Recibido, lote.Estado);
        Assert.Equal(2, lote.Resultados.Count);
        Assert.Contains(lote.DomainEvents, e => e is LoteResultadosRecibido);
    }

    [Fact]
    public void Recibir_rechaza_lote_sin_resultados()
    {
        Assert.Throws<ArgumentException>(() =>
            LoteResultados.Recibir(Tenant, Campana, "Lab", "k", []));
    }

    [Fact]
    public void Validar_transiciona_a_validado_y_emite_evento()
    {
        var lote = NuevoLote();

        lote.Validar();

        Assert.Equal(EstadoLote.Validado, lote.Estado);
        Assert.NotNull(lote.ValidadoUtc);
        Assert.Contains(lote.DomainEvents, e => e is LoteResultadosValidado);
    }

    [Fact]
    public void Validar_dos_veces_falla()
    {
        var lote = NuevoLote();
        lote.Validar();

        Assert.Throws<InvalidOperationException>(() => lote.Validar());
    }

    [Fact]
    public void Rechazar_requiere_motivo_y_marca_estado()
    {
        var lote = NuevoLote();

        lote.Rechazar("Unidades inconsistentes");

        Assert.Equal(EstadoLote.Rechazado, lote.Estado);
        Assert.Equal("Unidades inconsistentes", lote.MotivoRechazo);
    }

    [Fact]
    public void Rechazar_sin_motivo_falla()
    {
        var lote = NuevoLote();

        Assert.Throws<ArgumentException>(() => lote.Rechazar("  "));
    }

    [Fact]
    public void No_se_puede_validar_un_lote_rechazado()
    {
        var lote = NuevoLote();
        lote.Rechazar("motivo");

        Assert.Throws<InvalidOperationException>(() => lote.Validar());
    }

    [Fact]
    public void ResultadoParametro_exige_codigo_parametro_y_unidad()
    {
        Assert.Throws<ArgumentException>(() => ResultadoParametro.Crear("", "pH", 7, "pH", null));
        Assert.Throws<ArgumentException>(() => ResultadoParametro.Crear("MTR-1", "", 7, "pH", null));
        Assert.Throws<ArgumentException>(() => ResultadoParametro.Crear("MTR-1", "pH", 7, "", null));
    }
}
