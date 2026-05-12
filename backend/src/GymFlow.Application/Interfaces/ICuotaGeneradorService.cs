using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface ICuotaGeneradorService
{
    Task<Cuota> GenerarCuotaAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaEmision);
    Task<IReadOnlyList<Cuota>> GenerarCuotasRetroactivasAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaAlta);
}
