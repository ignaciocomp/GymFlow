---
tags:
  - diagrama
requerimiento: RF-06
related:
  - "[[spec-rf06-recordatorios-cuota]]"
---

```plantuml
@startuml Diagrama de Actividad - Recordatorios Automáticos (RF-06)

title Proceso de Recordatorios Automáticos de Cuota (RF-06)

start

:RecordatorioBackgroundService\nse activa a la hora configurada\n(default: 08:00 UTC);

if (Recordatorios:Habilitado?) then (no)
  :Log: servicio deshabilitado;
  stop
else (sí)
endif

:Consultar cuotas **Pendientes** con\nFechaVencimiento en {hoy, hoy+1, hoy+5};
note right
  ""GetCuotasParaRecordatorioAsync(hoy)""
  Incluye datos de Socio y Unidad
end note

if (¿Hay cuotas pendientes?) then (no)
  :Log: sin cuotas para procesar\n(enviados=0, omitidos=0, fallidos=0);
  stop
else (sí)
endif

partition "Por cada cuota pendiente" {

  :Resolver tipo de recordatorio;
  note right
    vence en 5 días → **CincoDias**
    vence en 1 día  → **UnDia**
    vence hoy       → **DiaVencimiento**
  end note

  if (¿Socio tiene email?) then (no)
    :Registrar RecordatorioCuota\n(exitoso=false, error="Socio sin correo");
    :omitidos++;
  else (sí)

    if (¿Ya se envió este tipo\nde recordatorio hoy\npara esta cuota?) then (sí)
      :omitidos++;
      note right
        RN-11: máx 1 recordatorio
        por tipo, por cuota, por día
        (previene spam)
      end note
    else (no)

      :Generar email HTML con\nEmailTemplates.Automatico();
      note right
        Valores dinámicos con
        HtmlEncode (anti-XSS)
        ──────────────────
        **CincoDias**: "Tu cuota vence pronto"
        **UnDia**: "Tu cuota vence mañana"
        **DiaVencimiento**: "Tu cuota vence hoy"
      end note

      :Enviar email vía SmtpEmailService;

      if (¿Envío exitoso?) then (sí)
        :Registrar RecordatorioCuota\n(exitoso=true);
        :enviados++;
      else (no)
        :Registrar RecordatorioCuota\n(exitoso=false, error=detalle);
        :fallidos++;
      endif

    endif
  endif
}

:Persistir todos los RecordatorioCuota\n(SaveChangesAsync);

:Log resumen:\nenviados / omitidos / fallidos;

:Calcular delay hasta próxima\nejecución (mañana a las 08:00);

stop

@enduml

````

