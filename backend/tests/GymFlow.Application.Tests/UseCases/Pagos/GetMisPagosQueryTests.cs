using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Pagos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Pagos;

public class GetMisPagosQueryTests
{
    private readonly Mock<IPagoRepository> _pagoRepo = new();

    private GetMisPagosQuery CrearQuery() => new(_pagoRepo.Object);

    private static Cuota CrearCuota(Guid socioId, string plan) =>
        new(socioId, Guid.NewGuid(), Guid.NewGuid(), plan, 2500m, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_MapeaPagosADtoConDatosDeLaCuota()
    {
        var socioId = Guid.NewGuid();
        var cuota = CrearCuota(socioId, "Plan Full");
        var pago = new Pago(cuota.Id, socioId, 2500m, "pref-1");
        pago.MarcarAprobado("mp-123", "credit_card");
        SetCuotaNav(pago, cuota);

        _pagoRepo.Setup(r => r.GetBySocioIdAsync(socioId)).ReturnsAsync(new[] { pago });

        var resultado = (await CrearQuery().ExecuteAsync(socioId)).ToList();

        var dto = Assert.Single(resultado);
        Assert.Equal(pago.Id, dto.Id);
        Assert.Equal(2500m, dto.Monto);
        Assert.Equal("credit_card", dto.MedioPago);
        Assert.Equal("mp-123", dto.MpPaymentId);
        Assert.Equal(EstadoPago.Aprobado, dto.Estado);
        Assert.Equal("Plan Full", dto.NombrePlan);
    }

    [Fact]
    public async Task ExecuteAsync_SinPagos_DevuelveVacio()
    {
        _pagoRepo.Setup(r => r.GetBySocioIdAsync(It.IsAny<Guid>())).ReturnsAsync(Array.Empty<Pago>());

        var resultado = await CrearQuery().ExecuteAsync(Guid.NewGuid());

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ExecuteAsync_RespetaElOrdenDelRepositorio_FechaDesc()
    {
        var socioId = Guid.NewGuid();
        var cuotaA = CrearCuota(socioId, "A");
        var cuotaB = CrearCuota(socioId, "B");
        var pagoNuevo = new Pago(cuotaA.Id, socioId, 100m, "pref-a");
        SetCuotaNav(pagoNuevo, cuotaA);
        var pagoViejo = new Pago(cuotaB.Id, socioId, 200m, "pref-b");
        SetCuotaNav(pagoViejo, cuotaB);

        // El repositorio ya devuelve ordenado desc; la query preserva ese orden.
        _pagoRepo.Setup(r => r.GetBySocioIdAsync(socioId)).ReturnsAsync(new[] { pagoNuevo, pagoViejo });

        var resultado = (await CrearQuery().ExecuteAsync(socioId)).ToList();

        Assert.Equal("A", resultado[0].NombrePlan);
        Assert.Equal("B", resultado[1].NombrePlan);
    }

    // La navegación Cuota tiene private set; en runtime la puebla EF con el Include.
    // Para el test se setea por reflexión.
    private static void SetCuotaNav(Pago pago, Cuota cuota) =>
        typeof(Pago).GetProperty(nameof(Pago.Cuota))!.SetValue(pago, cuota);
}
