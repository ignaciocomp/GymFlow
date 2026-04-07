using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Auditoria;

public class GetAuditoriaQuery
{
    private readonly IAuditLogRepository _repository;

    public GetAuditoriaQuery(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AuditoriaDto>> ExecuteAsync(
        DateTime? desde,
        DateTime? hasta,
        TipoAccionAuditoria? tipoAccion,
        Guid? entidadId)
    {
        var registros = await _repository.SearchAsync(desde, hasta, tipoAccion, entidadId);

        return registros.Select(r => new AuditoriaDto(
            Id: r.Id,
            UsuarioId: r.UsuarioId,
            UsuarioNombre: r.UsuarioNombre,
            TipoAccion: r.TipoAccion.ToString(),
            EntidadAfectada: r.EntidadAfectada,
            EntidadId: r.EntidadId,
            Descripcion: r.Descripcion,
            DetallesCambios: r.DetallesCambios,
            FechaHora: r.FechaHora));
    }
}
