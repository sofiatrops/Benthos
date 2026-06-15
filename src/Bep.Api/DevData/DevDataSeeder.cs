using System.Net.Http.Headers;
using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Storage;
using Bep.Infrastructure.Storage;
using Bep.Modules.Campaign.Application.Campanias.CrearCampana;
using Bep.Modules.Campaign.Domain;
using Bep.Modules.Insights.Application.Analisis;
using Bep.Modules.Insights.Application.Analisis.GenerarAnalisis;
using Bep.Modules.Laboratory.Application.LoteResultados;
using Bep.Modules.Laboratory.Application.LoteResultados.ImportarResultados;
using Bep.Modules.Organization.Application.Abstractions;
using Bep.Modules.Organization.Domain;
using Bep.Modules.Reporting.Application.Informes;
using Bep.Modules.Reporting.Application.Informes.CrearInforme;
using Bep.Modules.Reporting.Domain;
using MediatR;

namespace Bep.Api.DevData;

/// <summary>
/// Siembra datos de demostración en Development para que el Portal Cliente muestre
/// contenido real sin pasos manuales. Idempotente: no hace nada si la empresa demo
/// ya existe. La empresa se aprovisiona con un <c>Id</c> fijo que coincide con el
/// claim <c>tenant_id</c> del usuario de prueba <c>cliente</c> del realm de Keycloak.
/// </summary>
public static class DevDataSeeder
{
    /// <summary>Tenant demo; debe coincidir con el atributo del usuario `cliente` (ver docker/keycloak/bep-realm.json).</summary>
    public static readonly Guid EmpresaDemoId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    public static async Task SeedAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        var tenantContext = services.GetRequiredService<ITenantContext>();
        var empresaRepo = services.GetRequiredService<IEmpresaRepository>();

        if (await empresaRepo.GetByIdAsync(EmpresaDemoId, ct) is not null)
        {
            logger.LogInformation("Datos demo ya presentes (empresa {EmpresaId}); se omite la siembra.", EmpresaDemoId);
            return;
        }

        // Toda la siembra opera sobre el tenant demo (habilita RLS en centros/campañas/informes).
        tenantContext.SetTenant(EmpresaDemoId);

        await SeedEmpresaYCentrosAsync(services, ct);
        var centroIds = (await empresaRepo.GetByIdAsync(EmpresaDemoId, ct))!.Centros.Select(c => c.Id).ToList();

        var sender = services.GetRequiredService<ISender>();
        var campanaId = await SeedCampanasAsync(sender, centroIds, ct);
        await SeedInformePublicadoAsync(services, sender, centroIds[0], logger, ct);
        await SeedResultadosLaboratorioAsync(services, sender, campanaId, logger, ct);
        await SeedAnalisisValidadoAsync(sender, campanaId, logger, ct);

        logger.LogInformation("Datos demo sembrados para la empresa {EmpresaId}.", EmpresaDemoId);
    }

    private static async Task SeedEmpresaYCentrosAsync(IServiceProvider services, CancellationToken ct)
    {
        var empresaRepo = services.GetRequiredService<IEmpresaRepository>();
        var unitOfWork = services.GetRequiredService<IOrganizationUnitOfWork>();

        var empresa = Empresa.Provisionar(EmpresaDemoId, "Salmones del Sur S.A.", Rut.Create("76000001-9"), "Acuicultura");
        empresa.AgregarCentro("Centro Quemchi", "QMC-01", CoordenadasGps.Create(-42.143, -73.483), "Los Lagos");
        empresa.AgregarCentro("Centro Dalcahue", "DLC-02", CoordenadasGps.Create(-42.378, -73.651), "Los Lagos");

        await empresaRepo.AddAsync(empresa, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static async Task<Guid> SeedCampanasAsync(ISender sender, List<Guid> centroIds, CancellationToken ct)
    {
        var primera = await sender.Send(new CrearCampanaCommand(
            EmpresaDemoId, "Monitoreo invierno 2026", "Campaña de calidad de agua y bentos.",
            TipoCampania.Mixta, new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31), centroIds), ct);

        await sender.Send(new CrearCampanaCommand(
            EmpresaDemoId, "Caracterización Dalcahue", "Línea base de macroinvertebrados.",
            TipoCampania.Macroinvertebrados, new DateOnly(2026, 5, 15), new DateOnly(2026, 7, 15), [centroIds[1]]), ct);

        return primera.Value;
    }

    private static async Task SeedResultadosLaboratorioAsync(
        IServiceProvider services, ISender sender, Guid campanaId, ILogger logger, CancellationToken ct)
    {
        const string csv = """
            codigo_muestra,parametro,valor,unidad,metodo
            MTR-20260601-DEMO000001,Oxígeno disuelto,8.4,mg/L,SM 4500-O
            MTR-20260601-DEMO000001,pH,7.9,pH,SM 4500-H
            MTR-20260601-DEMO000002,Temperatura,12.6,°C,SM 2550
            MTR-20260601-DEMO000002,Nitrógeno total,1.2,mg/L,SM 4500-N
            """;

        var objectKey = await SubirAsync(services, CategoriasObjeto.Laboratorio, "resultados-demo.csv", "text/csv",
            System.Text.Encoding.UTF8.GetBytes(csv), logger, ct);

        var importado = await sender.Send(
            new ImportarResultadosCommand(EmpresaDemoId, campanaId, "Laboratorio Ambiental Austral", objectKey), ct);
        if (importado.IsFailure)
        {
            logger.LogWarning("No se pudieron sembrar resultados de laboratorio: {Error}", importado.Error!.Message);
            return;
        }

        // Validado: enciende los KPIs del portal (RF-04-005 / RF-07-005).
        await sender.Send(new ValidarLoteCommand(EmpresaDemoId, importado.Value.LoteId), ct);
    }

    private static async Task SeedAnalisisValidadoAsync(ISender sender, Guid campanaId, ILogger logger, CancellationToken ct)
    {
        // Genera el análisis de IA sobre los resultados validados y lo valida (humano en
        // el bucle, RF-06-007) para que el dashboard del cliente muestre el resumen.
        var generado = await sender.Send(new GenerarAnalisisCommand(EmpresaDemoId, campanaId), ct);
        if (generado.IsFailure)
        {
            logger.LogWarning("No se pudo sembrar el análisis de IA: {Error}", generado.Error!.Message);
            return;
        }

        await sender.Send(new ValidarAnalisisCommand(EmpresaDemoId, generado.Value), ct);
    }

    private static async Task SeedInformePublicadoAsync(
        IServiceProvider services, ISender sender, Guid centroId, ILogger logger, CancellationToken ct)
    {
        var pdf = "%PDF-1.4\n% Informe demo Benthos\n%%EOF"u8.ToArray();
        var objectKey = await SubirAsync(services, CategoriasObjeto.InformesPdf, "informe-demo.pdf", "application/pdf", pdf, logger, ct);

        var informeId = await sender.Send(new CrearInformeCommand(
            EmpresaDemoId, "Informe de calidad de agua — invierno 2026", TipoEstudio.CalidadAgua,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), null, centroId, objectKey), ct);

        // Recorre el flujo de revisión hasta dejarlo visible para el cliente (RF-05-003/005).
        await sender.Send(new TransicionarEstadoInformeCommand(EmpresaDemoId, informeId.Value, EstadoInforme.EnRevision), ct);
        await sender.Send(new TransicionarEstadoInformeCommand(EmpresaDemoId, informeId.Value, EstadoInforme.Aprobado), ct);
        await sender.Send(new TransicionarEstadoInformeCommand(EmpresaDemoId, informeId.Value, EstadoInforme.Publicado), ct);
    }

    /// <summary>
    /// Sube un artefacto demo al almacén con una URL firmada, para que la descarga e
    /// ingesta funcionen de verdad. Si el almacén no está disponible, registra el
    /// problema y devuelve igualmente una clave válida (la metadata seguirá demostrable).
    /// </summary>
    private static async Task<string> SubirAsync(
        IServiceProvider services, string categoria, string nombre, string contentType, byte[] contenido,
        ILogger logger, CancellationToken ct)
    {
        var storage = services.GetRequiredService<IObjectStorage>();
        var fallbackKey = ObjectKeys.Construir(EmpresaDemoId, categoria, nombre);

        try
        {
            // El inicializador de bucket es un IHostedService que aún no arrancó en este
            // punto del pipeline; aseguramos el bucket explícitamente antes de subir.
            await ObjectStorageProvisioner.EnsureBucketAsync(services, ct);

            var ticket = await storage.CrearTicketSubidaAsync(
                new SolicitudSubida(categoria, nombre, contentType, contenido.Length), ct);
            if (ticket.IsFailure)
            {
                logger.LogWarning("No se pudo emitir el ticket de subida demo: {Error}", ticket.Error!.Message);
                return fallbackKey;
            }

            using var http = new HttpClient();
            using var content = new ByteArrayContent(contenido);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            (await http.PutAsync(ticket.Value.UrlSubida, content, ct)).EnsureSuccessStatusCode();
            return ticket.Value.ObjectKey;
        }
#pragma warning disable CA1031 // Siembra de un artefacto opcional: nunca debe abortar el arranque.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogWarning(ex, "No se pudo subir el artefacto demo '{Nombre}' al almacén; la metadata se sembrará igual.", nombre);
            return fallbackKey;
        }
    }
}
