using Bep.Modules.Campaign.Domain;
using Bep.Modules.Campaign.Domain.Events;

namespace Bep.Modules.Campaign.Domain.Tests;

public sealed class CampaniaTests
{
    private static Campania NuevaCampania() => Campania.Crear(
        Guid.NewGuid(),
        "Campaña verano",
        "Monitoreo trimestral",
        TipoCampania.Mixta,
        RangoFechas.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)),
        [Guid.NewGuid()]);

    [Fact]
    public void Crear_starts_planificada_and_raises_event()
    {
        var campania = NuevaCampania();

        Assert.Equal(EstadoCampania.Planificada, campania.Estado);
        Assert.Contains(campania.DomainEvents, e => e is CampanaCreada);
    }

    [Fact]
    public void Crear_requires_at_least_one_centro()
    {
        Assert.Throws<ArgumentException>(() => Campania.Crear(
            Guid.NewGuid(), "X", "Y", TipoCampania.CalidadAgua,
            RangoFechas.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)), []));
    }

    [Fact]
    public void RangoFechas_rejects_fin_before_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            RangoFechas.Create(new DateOnly(2026, 3, 31), new DateOnly(2026, 1, 1)));
    }

    [Theory]
    [InlineData(EstadoCampania.EnCurso, true)]
    [InlineData(EstadoCampania.Cancelada, true)]
    [InlineData(EstadoCampania.Cerrada, false)]
    [InlineData(EstadoCampania.EnRevision, false)]
    public void Planificada_allows_only_valid_transitions(EstadoCampania destino, bool permitido)
    {
        var campania = NuevaCampania();

        Assert.Equal(permitido, campania.PuedeTransicionarA(destino));
    }

    [Fact]
    public void Transicionar_changes_state_and_raises_event()
    {
        var campania = NuevaCampania();

        campania.Transicionar(EstadoCampania.EnCurso);

        Assert.Equal(EstadoCampania.EnCurso, campania.Estado);
        Assert.Contains(campania.DomainEvents, e => e is EstadoCampanaCambiado);
    }

    [Fact]
    public void Transicionar_invalid_throws()
    {
        var campania = NuevaCampania();

        Assert.Throws<InvalidOperationException>(() => campania.Transicionar(EstadoCampania.Cerrada));
    }

    [Fact]
    public void Closing_campaign_raises_campana_cerrada()
    {
        var campania = NuevaCampania();
        campania.Transicionar(EstadoCampania.EnCurso);
        campania.Transicionar(EstadoCampania.EnRevision);
        campania.Transicionar(EstadoCampania.Cerrada);

        Assert.Equal(EstadoCampania.Cerrada, campania.Estado);
        Assert.Contains(campania.DomainEvents, e => e is CampanaCerrada);
    }

    [Fact]
    public void Cerrada_is_terminal()
    {
        var campania = NuevaCampania();
        campania.Transicionar(EstadoCampania.EnCurso);
        campania.Transicionar(EstadoCampania.EnRevision);
        campania.Transicionar(EstadoCampania.Cerrada);

        Assert.False(campania.PuedeTransicionarA(EstadoCampania.EnCurso));
        Assert.False(campania.PuedeTransicionarA(EstadoCampania.Cancelada));
    }

    [Fact]
    public void AsignarResponsables_replaces_responsables()
    {
        var campania = NuevaCampania();

        campania.AsignarResponsables([
            Responsable.Create("sub-1", "coordinador"),
            Responsable.Create("sub-2", "tecnico"),
        ]);

        Assert.Equal(2, campania.Responsables.Count);
    }
}
