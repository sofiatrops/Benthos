namespace Bep.Modules.Insights.Domain;

/// <summary>
/// Estado de un análisis de IA ambiental (RF-06). Nace como <see cref="Borrador"/>
/// generado automáticamente; un profesional lo <see cref="Validado"/> o lo
/// <see cref="Descartado"/>. Solo lo validado puede mostrarse al cliente (RF-06-007/010).
/// </summary>
public enum EstadoAnalisis
{
    Borrador = 1,
    Validado = 2,
    Descartado = 3,
}

/// <summary>Severidad de un hallazgo del análisis.</summary>
public enum SeveridadHallazgo
{
    Informativo = 1,
    Atencion = 2,
    Critico = 3,
}
