using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Infrastructure.Services;

public class CuotaGeneradorService : ICuotaGeneradorService
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IPlanRepository _planRepository;

    public CuotaGeneradorService(ICuotaRepository cuotaRepository, IPlanRepository planRepository)
    {
        _cuotaRepository = cuotaRepository;
        _planRepository = planRepository;
    }

    public async Task<Cuota> GenerarCuotaAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaEmision)
    {
        if (!usuarioUnidad.PlanId.HasValue)
            throw new InvalidOperationException("El socio no tiene plan asignado en esta unidad.");

        var plan = await _planRepository.GetByIdAsync(usuarioUnidad.PlanId.Value)
            ?? throw new InvalidOperationException("El plan asignado no existe.");

        var cuota = new Cuota(
            socioId: socioId,
            unidadId: usuarioUnidad.UnidadId,
            planId: plan.Id,
            nombrePlan: plan.Nombre,
            monto: plan.Precio,
            fechaEmision: fechaEmision);

        await _cuotaRepository.AddAsync(cuota);
        return cuota;
    }

    public async Task<IReadOnlyList<Cuota>> GenerarCuotasRetroactivasAsync(Guid socioId, UsuarioUnidad usuarioUnidad, DateTime fechaAlta)
    {
        if (!usuarioUnidad.PlanId.HasValue)
            throw new InvalidOperationException("El socio no tiene plan asignado en esta unidad.");

        var plan = await _planRepository.GetByIdAsync(usuarioUnidad.PlanId.Value)
            ?? throw new InvalidOperationException("El plan asignado no existe.");

        var existentes = await _cuotaRepository.SearchAsync(socioId, null, null, null, usuarioUnidad.UnidadId, incluirAnuladas: true);

        var cuotas = new List<Cuota>();
        var fechaEmision = fechaAlta;

        while (fechaEmision < DateTime.UtcNow)
        {
            var yaExiste = existentes.Any(c =>
                c.FechaEmision.Year == fechaEmision.Year &&
                c.FechaEmision.Month == fechaEmision.Month);

            if (!yaExiste)
            {
                var cuota = new Cuota(
                    socioId: socioId,
                    unidadId: usuarioUnidad.UnidadId,
                    planId: plan.Id,
                    nombrePlan: plan.Nombre,
                    monto: plan.Precio,
                    fechaEmision: fechaEmision);

                await _cuotaRepository.AddAsync(cuota);
                cuotas.Add(cuota);
            }

            fechaEmision = fechaEmision.AddMonths(1);
        }

        return cuotas;
    }
}
