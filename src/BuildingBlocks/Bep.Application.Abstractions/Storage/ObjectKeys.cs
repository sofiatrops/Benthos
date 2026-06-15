using System.Text.RegularExpressions;

namespace Bep.Application.Abstractions.Storage;

/// <summary>
/// Convención de claves de objeto del almacenamiento S3-compatible (ADR-008).
/// Toda clave vive bajo el prefijo del tenant — <c>{tenant_id}/{categoria}/...</c> —
/// para garantizar aislamiento lógico y permitir validar pertenencia sin consultar
/// el almacén. Funciones puras: reutilizables tanto por el adaptador como por los
/// validadores de comandos (defensa frente a IDOR).
/// </summary>
public static partial class ObjectKeys
{
    /// <summary>Prefijo de todas las claves de un tenant.</summary>
    public static string Prefijo(Guid tenantId) => $"{tenantId:D}/";

    /// <summary>
    /// Indica si una clave pertenece al tenant indicado. Una clave fuera del prefijo
    /// del tenant nunca debe aceptarse ni firmarse (cierra el vector IDOR del SRS).
    /// </summary>
    public static bool PerteneceA(string? objectKey, Guid tenantId) =>
        !string.IsNullOrWhiteSpace(objectKey) &&
        objectKey.StartsWith(Prefijo(tenantId), StringComparison.Ordinal);

    /// <summary>
    /// Construye una clave única y estable: <c>{tenant}/{categoria}/{guid}/{nombre-saneado}</c>.
    /// El GUID intermedio evita colisiones y enumeración; el nombre saneado preserva
    /// la extensión para descargas legibles.
    /// </summary>
    public static string Construir(Guid tenantId, string categoria, string nombreArchivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoria);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreArchivo);

        var categoriaSegura = Sanear(categoria);
        var nombreSeguro = Sanear(nombreArchivo);
        return $"{Prefijo(tenantId)}{categoriaSegura}/{Guid.NewGuid():N}/{nombreSeguro}";
    }

    /// <summary>
    /// Reduce un segmento a caracteres seguros para una clave S3 (sin separadores de
    /// ruta ni caracteres de control), conservando puntos y guiones de la extensión.
    /// </summary>
    private static string Sanear(string valor)
    {
        var limpio = CaracteresInseguros().Replace(valor.Trim(), "-").Trim('-', '.');
        return limpio.Length == 0 ? "archivo" : limpio;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9._-]+")]
    private static partial Regex CaracteresInseguros();
}
