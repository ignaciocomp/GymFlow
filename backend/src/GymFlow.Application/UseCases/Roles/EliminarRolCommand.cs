using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Roles;

public class EliminarRolCommand
{
    private readonly IRolRepository _rolRepository;
    private readonly IPermisoCache _cache;
    private readonly IAuditLogger _auditLogger;

    public EliminarRolCommand(IRolRepository rolRepository, IPermisoCache cache, IAuditLogger auditLogger)
    {
        _rolRepository = rolRepository;
        _cache = cache;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        var rol = await _rolRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rol {id} no encontrado.");

        if (rol.EsSistema)
            throw new InvalidOperationException("No se puede eliminar un rol del sistema.");

        if (await _rolRepository.TieneUsuariosActivosAsignadosAsync(id, ct))
            throw new InvalidOperationException("No se puede eliminar un rol con usuarios activos asignados.");

        _rolRepository.Remove(rol);
        await _rolRepository.SaveChangesAsync(ct);
        _cache.Invalidar(id);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Rol", id,
            $"Se eliminó el rol {rol.Nombre}");
    }
}
