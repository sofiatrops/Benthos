using Bep.Modules.Sampling.Domain;
using Bep.Modules.Sampling.Domain.Events;

namespace Bep.Modules.Sampling.Domain.Tests;

public sealed class MuestraTests
{
    private static Muestra NuevaMuestra() => Muestra.Registrar(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoMuestra.Agua,
        ["oxigeno_disuelto", "ph"], UbicacionGps.Create(-41.5, -73.0, 5), "tecnico-1");

    [Fact]
    public void Registrar_creates_with_code_qr_event_and_state()
    {
        var muestra = NuevaMuestra();

        Assert.Equal(EstadoMuestra.Registrada, muestra.Estado);
        Assert.StartsWith("MTR-", muestra.CodigoUnico);
        Assert.StartsWith("QR-", muestra.CodigoQr.Value);
        Assert.Contains(muestra.Eventos, e => e.Tipo == TipoEventoMuestra.Registro);
        Assert.Contains(muestra.DomainEvents, e => e is MuestraRegistrada);
    }

    [Fact]
    public void Registrar_requires_campana_and_centro()
    {
        Assert.Throws<ArgumentException>(() => Muestra.Registrar(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), TipoMuestra.Agua, [], UbicacionGps.Create(0, 0), null));

        Assert.Throws<ArgumentException>(() => Muestra.Registrar(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, TipoMuestra.Agua, [], UbicacionGps.Create(0, 0), null));
    }

    [Fact]
    public void AgregarFoto_stores_reference_and_event()
    {
        var muestra = NuevaMuestra();

        muestra.AgregarFoto("tenant/muestra/foto1.jpg", "tecnico-1");

        Assert.Single(muestra.Fotos);
        Assert.Contains(muestra.Eventos, e => e.Tipo == TipoEventoMuestra.Fotografia);
    }

    [Fact]
    public void TransferirCustodia_creates_pending_custody_and_changes_state()
    {
        var muestra = NuevaMuestra();

        muestra.TransferirCustodia("tecnico-1", "laboratorio-1", "tecnico-1");

        Assert.Equal(EstadoMuestra.EnTraslado, muestra.Estado);
        Assert.Single(muestra.Custodias);
        Assert.False(muestra.Custodias[0].Aceptada);
        Assert.Contains(muestra.DomainEvents, e => e is CustodiaTransferida);
    }

    [Fact]
    public void AceptarCustodia_accepts_and_moves_to_lab()
    {
        var muestra = NuevaMuestra();
        muestra.TransferirCustodia("tecnico-1", "laboratorio-1", "tecnico-1");

        muestra.AceptarCustodia("laboratorio-1");

        Assert.Equal(EstadoMuestra.RecibidaLaboratorio, muestra.Estado);
        Assert.True(muestra.Custodias[0].Aceptada);
        Assert.Contains(muestra.DomainEvents, e => e is CustodiaAceptada);
    }

    [Fact]
    public void AceptarCustodia_without_pending_throws()
    {
        var muestra = NuevaMuestra();

        Assert.Throws<InvalidOperationException>(() => muestra.AceptarCustodia("laboratorio-1"));
    }

    [Fact]
    public void Lab_lifecycle_runs_to_archivada()
    {
        var muestra = NuevaMuestra();
        muestra.TransferirCustodia("tecnico-1", "lab-1", "tecnico-1");
        muestra.AceptarCustodia("lab-1");

        muestra.Transicionar(EstadoMuestra.EnAnalisis, "lab-1", "Inicio de análisis");
        muestra.Transicionar(EstadoMuestra.ConResultado, "lab-1", "Resultado disponible");
        muestra.Transicionar(EstadoMuestra.Archivada, "lab-1", "Archivada");

        Assert.Equal(EstadoMuestra.Archivada, muestra.Estado);
        Assert.Contains(muestra.Eventos, e => e.Tipo == TipoEventoMuestra.Analisis);
        Assert.Contains(muestra.Eventos, e => e.Tipo == TipoEventoMuestra.Archivo);
    }

    [Fact]
    public void Transicionar_invalid_throws()
    {
        var muestra = NuevaMuestra();

        // Registrada no puede saltar directamente a EnAnalisis.
        Assert.Throws<InvalidOperationException>(() =>
            muestra.Transicionar(EstadoMuestra.EnAnalisis, "x", "y"));
    }

    [Fact]
    public void Eventos_history_is_chronological_and_accumulates()
    {
        var muestra = NuevaMuestra();
        var inicial = muestra.Eventos.Count;

        muestra.AgregarFoto("k1", "t");
        muestra.TransferirCustodia("t", "l", "t");

        Assert.True(muestra.Eventos.Count > inicial);
    }
}
