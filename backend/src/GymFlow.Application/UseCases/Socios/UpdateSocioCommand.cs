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
    private readonly ICuotaRepository _cuotaRepository;
    private readonly ICuotaGeneradorService _cuotaGenerador;

    public UpdateSocioCommand(
        ISocioRepository socioRepository,
        IUnidadRepository unidadRepository,
        IPlanRepository planRepository,
        IAuditLogger auditLogger,
        ICuotaRepository cuotaRepository,
        ICuotaGeneradorService cuotaGenerador)
    {
        _socioRepository = socioRepository;
        _unidadRepository = unidadRepository;
        _planRepository = planRepository;
        _auditLogger = auditLogger;
        _cuotaRepository = cuotaRepository;
        _cuotaGenerador = cuotaGenerador;
    }

    public async Task<SocioDto> ExecuteAsync(Guid id, UpdateSocioRequest request, Guid usuarioId, string usuarioNombre)
    {
        var socio = await _socioRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el socio con ID {id}.");

        if (!string.Equals(socio.Correo, request.Correo, StringComparison.OrdinalIgnoreCase))
        {
            if (await _socioRepository.ExisteCorreoAsync(request.Correo))
                throw new InvalidOperationException("El correo ingresado ya está registrado.");
        }

        var unidades = request.Unidades?.ToList() ?? [];
        foreach (var asignacion in unidades)
        {
            var unidad = await _unidadRepository.GetByIdAsync(asignacion.UnidadId);
            if (unidad == null)
                throw new ArgumentException($"La unidad con ID {asignacion.UnidadId} no existe.");

            if (asignacion.PlanId.HasValue)
            {
                var plan = await _planRepository.GetByIdAsync(asignacion.PlanId.Value);
                if (plan == null || !plan.EstaActivo)
                    throw new ArgumentException("El plan seleccionado no existe o no está activo.");
                if (plan.UnidadId != asignacion.UnidadId)
                    throw new ArgumentException($"El plan seleccionado no pertenece a la unidad {unidad.Nombre}.");
            }
        }

        var cambios = new Dictionary<string, object?>();
        if (socio.Nombre != request.Nombre) cambios["Nombre"] = new { anterior = socio.Nombre, nuevo = request.Nombre };
        if (socio.Apellido != request.Apellido) cambios["Apellido"] = new { anterior = socio.Apellido, nuevo = request.Apellido };
        if (socio.Correo != request.Correo) cambios["Correo"] = new { anterior = socio.Correo, nuevo = request.Correo };
        if (socio.Telefono != request.Telefono) cambios["Telefono"] = new { anterior = socio.Telefono, nuevo = request.Telefono };
        if (socio.TipoDocumento != request.TipoDocumento) cambios["TipoDocumento"] = new { anterior = socio.TipoDocumento.ToString(), nuevo = request.TipoDocumento.ToString() };
        if (socio.DocumentoIdentidad != request.DocumentoIdentidad) cambios["DocumentoIdentidad"] = new { anterior = socio.DocumentoIdentidad, nuevo = request.DocumentoIdentidad };

        socio.ActualizarDatosSocio(
            nombre: request.Nombre,
            apellido: request.Apellido,
            correo: request.Correo,
            tipoDocumento: request.TipoDocumento,
            telefono: request.Telefono,
            documentoIdentidad: request.DocumentoIdentidad,
            fechaNacimiento: request.FechaNacimiento.HasValue
                ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
                : null);

        var fechaAltaCambio = false;
        if (request.FechaAlta.HasValue)
        {
            var nuevaFechaAlta = DateTime.SpecifyKind(request.FechaAlta.Value, DateTimeKind.Utc);
            if (socio.FechaAlta != nuevaFechaAlta)
            {
                cambios["FechaAlta"] = new { anterior = socio.FechaAlta, nuevo = nuevaFechaAlta };
                socio.ActualizarFechaAlta(nuevaFechaAlta);
                fechaAltaCambio = true;
            }
        }

        socio.UnidadesAsignadas.Clear();
        foreach (var asignacion in unidades)
        {
            socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, asignacion.UnidadId, asignacion.PlanId));
        }

        await _socioRepository.SaveChangesAsync();

        if (fechaAltaCambio)
        {
            await _cuotaRepository.DeletePendientesBySocioAsync(socio.Id);
            await _cuotaRepository.SaveChangesAsync();

            foreach (var asignacion in unidades)
            {
                if (asignacion.PlanId.HasValue)
                {
                    var uu = socio.UnidadesAsignadas.First(u => u.UnidadId == asignacion.UnidadId);
                    await _cuotaGenerador.GenerarCuotasRetroactivasAsync(socio.Id, uu, socio.FechaAlta);
                }
            }
            await _cuotaRepository.SaveChangesAsync();
        }

        string? detallesJson = cambios.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(cambios)
            : null;

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Socio", id,
            $"Se modificaron los datos del socio {request.Nombre} {request.Apellido}",
            detallesJson);

        var updated = await _socioRepository.GetByIdAsync(id);
        return CreateSocioCommand.MapToDto(updated!);
    }
}
