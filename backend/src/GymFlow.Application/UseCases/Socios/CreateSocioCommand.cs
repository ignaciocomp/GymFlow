using System.ComponentModel;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Socios;

public class CreateSocioCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IPlanRepository _planRepository;

    public CreateSocioCommand(
        ISocioRepository socioRepository,
        IUnidadRepository unidadRepository,
        IPlanRepository planRepository)
    {
        _socioRepository = socioRepository;
        _unidadRepository = unidadRepository;
        _planRepository = planRepository;
    }

    public async Task<SocioDto> ExecuteAsync(CreateSocioRequest request)
    {

        // Validate número de cédula único
        if(request.DocumentoIdentidad != null && await _socioRepository.ExisteCedulaAsync(request.DocumentoIdentidad))
            throw new InvalidOperationException("El número de cédula ya está registrado");

        // Validate unique email (RN-05, RN-29)
        if (await _socioRepository.ExisteCorreoAsync(request.Correo))
            throw new InvalidOperationException("El correo ingresado ya está registrado.");

        // Validate at least one unidad
        var uniqueUnidadIds = request.UnidadIds?.Distinct().ToList() ?? [];
        if (uniqueUnidadIds.Count == 0)
            throw new ArgumentException("Debe asignar al menos una unidad.");

        // Validate unidades exist
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

        // Create socio (domain validates consentimiento - Ley 18.331)
        var socio = new Socio(
            nombre: request.Nombre,
            apellido: request.Apellido,
            correo: request.Correo,
            passwordHash: "PENDING_OAUTH",
            planId: request.PlanId,
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: request.ConsentimientoInformado,
            tipoDocumento: request.TipoDocumento,
            telefono: request.Telefono,
            documentoIdentidad: request.DocumentoIdentidad,
            fechaNacimiento: request.FechaNacimiento.HasValue
                ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
                : null);

        // Assign unidades (RN-01: socio puede pertenecer a uno o ambos espacios)
        foreach (var unidadId in uniqueUnidadIds)
        {
            socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, unidadId));
        }

        await _socioRepository.AddAsync(socio);
        await _socioRepository.SaveChangesAsync();

        // Re-fetch to load navigation properties (Plan, Unidades)
        var saved = await _socioRepository.GetByIdAsync(socio.Id);
        return MapToDto(saved!);
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
