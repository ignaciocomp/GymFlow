using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Socios;

public class ReactivateSocioCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IAuditLogger _auditLogger;

    public ReactivateSocioCommand(ISocioRepository socioRepository, IAuditLogger auditLogger)
    {
        _socioRepository = socioRepository;
        _auditLogger = auditLogger;
    }

    public async Task<SocioDto> ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var socio = await _socioRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el socio con ID {id}.");

        socio.Reactivar();

        await _socioRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId,
            usuarioNombre,
            TipoAccionAuditoria.Reactivacion,
            "Socio",
            id,
            $"Se reactivó al socio {socio.Nombre} {socio.Apellido}");

        return MapToDto(socio);
    }

    private static SocioDto MapToDto(Socio socio) => CreateSocioCommand.MapToDto(socio);
}
