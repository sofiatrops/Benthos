using Bep.Application.Abstractions.Messaging;

namespace Bep.Modules.Organization.Application.Centros.RegistrarCentro;

/// <summary>Registra un centro operativo asociado a una empresa. RF-01-002.</summary>
public sealed record RegistrarCentroCommand(
    Guid EmpresaId,
    string Nombre,
    string CodigoInterno,
    double Latitud,
    double Longitud,
    string Region) : ICommand<Guid>;
