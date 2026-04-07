using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Socios;

public class UpdateSocioCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public UpdateSocioCommand(
        ISocioRepository socioRepository,
        IUnidadRepository unidadRepository,
        IPlanRepository planRepository,
        IAuditLogger auditLogger)
    {
        _socioRepository = socioRepository;
        _unidadRepository = unidadRepository;
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task<SocioDto> ExecuteAsync(Guid id, UpdateSocioRequest request, Guid usuarioId, string usuarioNombre)
    {
        var socio = await _socioRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el socio con ID {id}.");

        // Validate unique email (skip if unchanged)
        if (!string.Equals(socio.Correo, request.Correo, StringComparison.OrdinalIgnoreCase))
        {
            if (await _socioRepository.ExisteCorreoAsync(request.Correo))
                throw new InvalidOperationException("El correo ingresado ya está registrado.");
        }

        // Deduplicate and validate unidades exist
        var uniqueUnidadIds = request.UnidadIds?.Distinct().ToList() ?? [];
        foreach (var unidadId in uniqueUnidadIds)
        {
            var unidad = await _unidadRepository.GetByIdAsync(unidadId);
            if (unidad == null)
                throw new ArgumentException($"La unidad con ID {unidadId} no existe.");
        }

        // Validate plan exists if provided
        if (request.PlanId.HasValue)
        {
            var plan = await _planRepository.GetByIdAsync(request.PlanId.Value);
            if (plan == null)
                throw new ArgumentException("El plan seleccionado no existe.");
        }

        // Capture old values for audit log
        var cambios = new Dictionary<string, object?>();
        if (socio.Nombre != request.Nombre) cambios["Nombre"] = new { anterior = socio.Nombre, nuevo = request.Nombre };
        if (socio.Apellido != request.Apellido) cambios["Apellido"] = new { anterior = socio.Apellido, nuevo = request.Apellido };
        if (socio.Correo != request.Correo) cambios["Correo"] = new { anterior = socio.Correo, nuevo = request.Correo };
        if (socio.Telefono != request.Telefono) cambios["Telefono"] = new { anterior = socio.Telefono, nuevo = request.Telefono };
        if (socio.TipoDocumento != request.TipoDocumento) cambios["TipoDocumento"] = new { anterior = socio.TipoDocumento.ToString(), nuevo = request.TipoDocumento.ToString() };
        if (socio.DocumentoIdentidad != request.DocumentoIdentidad) cambios["DocumentoIdentidad"] = new { anterior = socio.DocumentoIdentidad, nuevo = request.DocumentoIdentidad };
        if (socio.PlanId != request.PlanId) cambios["PlanId"] = new { anterior = socio.PlanId, nuevo = request.PlanId };

        // Update socio data
        socio.ActualizarDatosSocio(
            nombre: request.Nombre,
            apellido: request.Apellido,
            correo: request.Correo,
            planId: request.PlanId,
            tipoDocumento: request.TipoDocumento,
            telefono: request.Telefono,
            documentoIdentidad: request.DocumentoIdentidad,
            fechaNacimiento: request.FechaNacimiento.HasValue
                ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
                : null);

        // Clear and re-assign unidades
        socio.UnidadesAsignadas.Clear();
        foreach (var unidadId in uniqueUnidadIds)
        {
            socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, unidadId));
        }

        await _socioRepository.SaveChangesAsync();

        string? detallesJson = cambios.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(cambios)
            : null;

        await _auditLogger.LogAsync(
            usuarioId,
            usuarioNombre,
            TipoAccionAuditoria.Modificacion,
            "Socio",
            id,
            $"Se modificaron los datos del socio {request.Nombre} {request.Apellido}",
            detallesJson);

        // Re-fetch to load navigation properties
        var updated = await _socioRepository.GetByIdAsync(id);
        return MapToDto(updated!);
    }

    private static SocioDto MapToDto(Socio socio)
    {
        return new SocioDto(
            Id: socio.Id,
            Nombre: socio.Nombre,
            Apellido: socio.Apellido,
            Correo: socio.Correo,
            Telefono: socio.Telefono,
            TipoDocumento: socio.TipoDocumento,
            DocumentoIdentidad: socio.DocumentoIdentidad,
            FechaNacimiento: socio.FechaNacimiento,
            FechaAlta: socio.FechaAlta,
            EstaActivo: socio.EstaActivo,
            PlanId: socio.PlanId,
            PlanNombre: socio.Plan?.Nombre,
            Unidades: socio.UnidadesAsignadas
                .Select(uu => new UnidadDto(uu.UnidadId, uu.Unidad?.Nombre ?? "", uu.Unidad?.Direccion ?? ""))
                .ToList());
    }
}
