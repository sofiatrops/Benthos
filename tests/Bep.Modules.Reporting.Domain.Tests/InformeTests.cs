using Bep.Modules.Reporting.Domain;
using Bep.Modules.Reporting.Domain.Events;

namespace Bep.Modules.Reporting.Domain.Tests;

public sealed class InformeTests
{
    private static Informe NuevoInforme() => Informe.Crear(
        Guid.NewGuid(), "Informe de calidad de agua Q1", TipoEstudio.CalidadAgua,
        PeriodoCubierto.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)),
        Guid.NewGuid(), Guid.NewGuid(), "autor-1", "tenant/informe/v1.pdf");

    private static Informe InformePublicado()
    {
        var informe = NuevoInforme();
        informe.Transicionar(EstadoInforme.EnRevision);
        informe.Transicionar(EstadoInforme.Aprobado);
        informe.Transicionar(EstadoInforme.Publicado);
        return informe;
    }

    [Fact]
    public void Crear_starts_as_borrador_with_first_version_and_event()
    {
        var informe = NuevoInforme();

        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
        Assert.Single(informe.Versiones);
        Assert.Equal(1, informe.VersionVigenteNumero);
        Assert.Contains(informe.DomainEvents, e => e is InformeCreado);
    }

    [Fact]
    public void CargarVersion_increments_and_keeps_previous()
    {
        var informe = NuevoInforme();

        informe.CargarVersion("tenant/informe/v2.pdf", "autor-1");

        Assert.Equal(2, informe.Versiones.Count);
        Assert.Equal(2, informe.VersionVigenteNumero);
        Assert.Contains(informe.DomainEvents, e => e is VersionInformeCargada);
    }

    [Fact]
    public void Review_flow_runs_to_publicado_and_sets_approval_date()
    {
        var informe = NuevoInforme();

        informe.Transicionar(EstadoInforme.EnRevision);
        informe.Transicionar(EstadoInforme.Aprobado);
        Assert.NotNull(informe.FechaAprobacionUtc);

        informe.Transicionar(EstadoInforme.Publicado);

        Assert.Equal(EstadoInforme.Publicado, informe.Estado);
        Assert.True(informe.EsVisibleParaCliente);
        Assert.Contains(informe.DomainEvents, e => e is InformePublicado);
    }

    [Fact]
    public void Cambios_solicitados_returns_to_revision()
    {
        var informe = NuevoInforme();
        informe.Transicionar(EstadoInforme.EnRevision);

        informe.Transicionar(EstadoInforme.CambiosSolicitados);
        Assert.True(informe.PuedeTransicionarA(EstadoInforme.EnRevision));
    }

    [Fact]
    public void Invalid_transition_throws()
    {
        var informe = NuevoInforme();

        // Borrador no puede pasar directo a Publicado.
        Assert.Throws<InvalidOperationException>(() => informe.Transicionar(EstadoInforme.Publicado));
    }

    [Fact]
    public void Cannot_upload_version_when_published()
    {
        var informe = InformePublicado();

        Assert.Throws<InvalidOperationException>(() => informe.CargarVersion("v2.pdf", "autor-1"));
    }

    [Fact]
    public void Published_report_is_not_visible_before_publication()
    {
        var informe = NuevoInforme();
        Assert.False(informe.EsVisibleParaCliente);

        informe.Transicionar(EstadoInforme.EnRevision);
        Assert.False(informe.EsVisibleParaCliente);
    }

    [Fact]
    public void Archivar_is_logical_deletion_and_raises_event()
    {
        var informe = InformePublicado();

        informe.Archivar();

        Assert.Equal(EstadoInforme.Archivado, informe.Estado);
        Assert.False(informe.EsVisibleParaCliente);
        Assert.Contains(informe.DomainEvents, e => e is InformeArchivado);
    }

    [Fact]
    public void Comentarios_and_anexos_accumulate()
    {
        var informe = NuevoInforme();

        informe.AgregarComentarioInterno("revisor-1", "Revisar tabla 3.");
        informe.AgregarAnexo("tenant/anexo/fotos.zip", "Anexo fotográfico");

        Assert.Single(informe.Comentarios);
        Assert.Single(informe.Anexos);
    }
}
