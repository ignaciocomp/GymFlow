using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Socios;

public class DeleteSocioCommand
{
    private readonly ISocioRepository _repository;

    public DeleteSocioCommand(ISocioRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(Guid socioId, string? motivo)
    {
        var socio = await _repository.GetByIdAsync(socioId);

        if (socio == null)
            throw new KeyNotFoundException($"Socio with ID {socioId} not found.");

        if (!socio.EstaActivo)
            throw new InvalidOperationException("El socio ya está dado de baja.");

        // Soft delete (RN-02: baja lógica, no se eliminan registros)
        socio.DarDeBaja(motivo);

        await _repository.SaveChangesAsync();
    }
}
