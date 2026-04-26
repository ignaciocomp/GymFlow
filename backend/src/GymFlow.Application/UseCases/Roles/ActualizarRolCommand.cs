using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Roles;

public class ActualizarRolCommand
{
    private readonly IRolRepository _rolRepository;
    private readonly IPermisoCache _cache;
    private readonly IAuditLogger _auditLogger;

    public ActualizarRolCommand(IRolRepository rolRepository, IPermisoCache cache, IAuditLogger auditLogger)
    {
        _rolRepository = rolRepository;
        _cache = cache;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, ActualizarRolRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        var rol = await _rolRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rol {id} no encontrado.");

        if (rol.EsSistema)
            throw new InvalidOperationException("No se puede modificar un rol del sistema.");

        if (await _rolRepository.ExisteConNombreAsync(request.Nombre, id, ct))
            throw new InvalidOperationException($"Ya existe otro rol con el nombre '{request.Nombre}'.");

        rol.Renombrar(request.Nombre);
        rol.ReemplazarPermisos(request.PermisoIds ?? new List<Guid>());

        await _rolRepository.SaveChangesAsync(ct);
        _cache.Invalidar(rol.Id);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Rol", rol.Id,
            $"Se modificó el rol {rol.Nombre} ({rol.Permisos.Count} permisos)");
    }
}
