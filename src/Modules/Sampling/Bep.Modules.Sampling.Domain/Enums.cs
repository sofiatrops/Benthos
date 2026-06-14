namespace Bep.Modules.Sampling.Domain;

/// <summary>Tipo de muestra recolectada (RF-03-005).</summary>
public enum TipoMuestra
{
    Agua = 1,
    Sedimento = 2,
    Macroinvertebrados = 3,
    Microinvertebrados = 4,
}

/// <summary>Estado de la muestra a lo largo de su ciclo de vida (RF-03-006).</summary>
public enum EstadoMuestra
{
    Registrada = 1,
    EnTraslado = 2,
    RecibidaLaboratorio = 3,
    EnAnalisis = 4,
    ConResultado = 5,
    Archivada = 6,
}

/// <summary>Tipo de evento en el historial cronológico de la muestra (RF-03-006).</summary>
public enum TipoEventoMuestra
{
    Registro = 1,
    Fotografia = 2,
    Traslado = 3,
    Recepcion = 4,
    Analisis = 5,
    Resultado = 6,
    Archivo = 7,
}
