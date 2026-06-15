namespace Bep.Modules.Laboratory.Domain;

/// <summary>
/// Estado de un lote de resultados de laboratorio (RF-04). El lote se recibe, un
/// profesional lo valida (o lo rechaza) y solo entonces sus parámetros alimentan
/// los indicadores del portal.
/// </summary>
public enum EstadoLote
{
    Recibido = 1,
    Validado = 2,
    Rechazado = 3,
}
