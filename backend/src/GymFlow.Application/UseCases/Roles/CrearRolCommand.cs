using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Roles;

public class CrearRolCommand
{
    private readonly IRolRepository _rolRepository;
    private readonly IAuditLogger _auditLogger;

    public CrearRolCommand(IRolRepository rolRepository, IAuditLogger auditLogger)
    {
        _rolRepository = rolRepository;
        _auditLogger = auditLogger;
    }

    public async Task<RolDto> ExecuteAsync(CrearRolRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(request));

        if (await _rolRepository.ExisteConNombreAsync(request.Nombre, null, ct))
            throw new InvalidOperationException($"Ya existe un rol con el nombre '{request.Nombre}'.");

        var rol = new Rol(request.Nombre);
        rol.ReemplazarPermisos(request.PermisoIds ?? new List<Guid>());

        await _rolRepository.AddAsync(rol, ct);
        await _rolRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Rol", rol.Id,
            $"Se creó el rol {rol.Nombre} con {rol.Permisos.Count} permisos");

        return new RolDto(rol.Id, rol.Nombre, rol.EsSistema, rol.FechaCreacion,
            rol.Permisos.Select(rp => new PermisoDto(rp.PermisoId, default, default)).ToList());
    }
}
