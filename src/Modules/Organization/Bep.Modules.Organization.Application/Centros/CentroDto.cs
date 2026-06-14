namespace Bep.Modules.Organization.Application.Centros;

public sealed record CentroDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    string CodigoInterno,
    double Latitud,
    double Longitud,
    string Region,
    bool Activo);
