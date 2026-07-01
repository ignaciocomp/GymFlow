using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Pagos;

/// <summary>
/// RF-23 / CU-08: el socio inicia el pago online de una cuota pendiente.
/// Valida que la cuota exista, sea del socio autenticado y esté Pendiente (E4),
/// crea un <see cref="Pago"/> Pendiente, genera la preferencia de Checkout Pro en MP
/// y devuelve el init_point al que redirigir. Si MP falla al crear la preferencia (E5),
/// el error se propaga.
/// </summary>
public class IniciarPagoCuotaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly IPagoUrlBuilder _urlBuilder;

    public IniciarPagoCuotaCommand(
        ICuotaRepository cuotaRepository,
        IPagoRepository pagoRepository,
        IMercadoPagoService mercadoPagoService,
        IPagoUrlBuilder urlBuilder)
    {
        _cuotaRepository = cuotaRepository;
        _pagoRepository = pagoRepository;
        _mercadoPagoService = mercadoPagoService;
        _urlBuilder = urlBuilder;
    }

    public async Task<IniciarPagoResultado> ExecuteAsync(Guid cuotaId, Guid socioId)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        // El socio solo puede pagar sus propias cuotas.
        if (cuota.SocioId != socioId)
            throw new UnauthorizedAccessException("La cuota no pertenece al socio.");

        // E4: solo se puede iniciar el pago de una cuota Pendiente (no Pagada/Anulada).
        if (cuota.Estado != EstadoCuota.Pendiente)
            throw new InvalidOperationException($"No se puede pagar una cuota en estado {cuota.Estado}.");

        // Se persiste primero el Pago para tener su Id, que se usa como external_reference en MP.
        var pago = new Pago(cuota.Id, socioId, cuota.Monto, string.Empty);
        await _pagoRepository.AddAsync(pago);
        await _pagoRepository.SaveChangesAsync();

        // E5: si MP falla acá, el error se propaga (el Pago queda Pendiente, sin preferencia).
        var preferencia = await _mercadoPagoService.CrearPreferenciaAsync(
            pago.Id,
            cuota.Monto,
            $"Cuota {cuota.NombrePlan}",
            _urlBuilder.BuildNotificationUrl(),
            _urlBuilder.BuildBackUrls());

        pago.SetMpPreferenceId(preferencia.PreferenceId);
        await _pagoRepository.SaveChangesAsync();

        return new IniciarPagoResultado(preferencia.InitPoint);
    }
}

public record IniciarPagoResultado(string InitPoint);
