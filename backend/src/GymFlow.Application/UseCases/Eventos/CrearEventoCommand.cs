using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Eventos;

public class CrearEventoCommand
{
    private readonly IEventoRepository _eventoRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;
    private readonly INotificadorInApp _notificador;

    public CrearEventoCommand(
        IEventoRepository eventoRepository,
        IUnidadRepository unidadRepository,
        ISocioRepository socioRepository,
        IAuditLogger auditLogger,
        IEmailService emailService,
        INotificadorInApp notificador)
    {
        _eventoRepository = eventoRepository;
        _unidadRepository = unidadRepository;
        _socioRepository = socioRepository;
        _auditLogger = auditLogger;
        _emailService = emailService;
        _notificador = notificador;
    }

    public async Task<EventoDto> ExecuteAsync(CreateEventoRequest request, Guid usuarioId, string usuarioNombre)
    {
        var unidad = await _unidadRepository.GetByIdAsync(request.UnidadId)
            ?? throw new ArgumentException("La unidad seleccionada no existe.");

        // Normalizar a UTC (Npgsql timestamptz exige Kind=Utc).
        var fechaUtc = DateTime.SpecifyKind(request.Fecha.ToUniversalTime(), DateTimeKind.Utc);

        if (fechaUtc < DateTime.UtcNow)
            throw new ArgumentException("La fecha del evento no puede ser pasada.", nameof(request));

        // El ctor valida el título no vacío (ArgumentException).
        var evento = new Evento(request.Titulo, request.Descripcion ?? "", fechaUtc, request.UnidadId);

        await _eventoRepository.AddAsync(evento);
        await _eventoRepository.SaveChangesAsync();

        // Persistir+auditar ANTES de enviar: el evento queda creado aunque los emails fallen.
        var socios = await _socioRepository.GetActivosByUnidadAsync(request.UnidadId);
        var resultado = await EventoNotificador.NotificarAsync(_emailService, socios, evento, unidad.Nombre);

        var detalle = resultado.Fallidos > 0
            ? $"Se creó el evento '{evento.Titulo}' en {unidad.Nombre}. Se notificaron {resultado.Enviados} de {resultado.Total} socios ({resultado.Fallidos} envíos fallaron)."
            : $"Se creó el evento '{evento.Titulo}' en {unidad.Nombre}. Se notificaron {resultado.Enviados} socios.";

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Evento", evento.Id,
            detalle);

        // Notificación in-app a los socios activos de la unidad tras el save de negocio.
        // Best-effort: si la creación falla, el evento igual queda creado.
        var sociosList = socios.ToList();
        if (sociosList.Count > 0)
        {
            try
            {
                var socioIds = sociosList.Select(s => s.Id);
                await _notificador.CrearParaVariosAsync(
                    socioIds,
                    TipoNotificacion.EventoNuevo,
                    $"Nuevo evento: {evento.Titulo}",
                    $"Se creó un nuevo evento en {unidad.Nombre}: {evento.Titulo} el {evento.Fecha:dd/MM/yyyy HH:mm}.");
            }
            catch
            {
                // Best-effort: la creación de las notificaciones in-app nunca rompe la creación del evento.
            }
        }

        // Poblar la navegación para el DTO (la unidad ya la tenemos cargada).
        return new EventoDto(evento.Id, evento.Titulo, evento.Descripcion, evento.Fecha,
            evento.UnidadId, unidad.Nombre, evento.EstaActivo);
    }
}
