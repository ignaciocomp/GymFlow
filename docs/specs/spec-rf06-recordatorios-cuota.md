---
tags:
  - spec
requerimiento: RF-06
---

# RF-06 — Recordatorios Automáticos de Cuota

**Estado:** Implementado (Iteración 2)
**Requerimientos:** [[GymFlow_Requerimientos_Completos]]
**Diagrama:** [[actividad_recordatorios_rf06]]
**Última actualización:** 2026-05-30
**Historial:**
- 2026-05-30 — Versión inicial (documentado a partir de implementación existente)

---

## Resumen

Implementar un sistema de notificaciones por email que permita al admin enviar recordatorios manuales individuales por cuota pendiente, y que el sistema envíe recordatorios automáticos antes del vencimiento de cada cuota.

---

## Alcance

### Incluido

- Infraestructura de email (servicio abstracto, configuración SMTP en `appsettings.json`).
- Servicio de email deshabiliteable en configuración para desarrollo/testing.
- Botón "Notificar" individual por cuota pendiente en la vista admin.
- Recordatorios automáticos por email: 5 días antes, 1 día antes, y el día del vencimiento.
- Entidad `RecordatorioCuota` para registro de notificaciones enviadas (evitar duplicados).

### Fuera de alcance

- Notificaciones in-app (campanita/push) — RF-16.
- Notificación masiva ("notificar a todos").
- Recordatorios post-vencimiento (ej. a 7 días de mora).

---

## Decisiones de diseño

- **Envío no bloqueante:** el envío de email no bloquea la respuesta del endpoint ni del background service.
- **Sin duplicados por tipo/día:** no se envía más de un recordatorio del mismo tipo por socio por día.
- **Socio sin correo:** se omite el envío y queda registrado en el sistema.
- **Solo cuotas pendientes:** los recordatorios solo se envían para cuotas en estado Pendiente.
- **Configurable:** el servicio de email se puede deshabilitar en configuración para desarrollo/testing.

---

## 1. Botón "Notificar" (manual del admin)

- Por cuota individual, en la vista admin de Gestión de Cuotas.
- Envía email al socio recordándole que tiene una cuota pendiente.
- El email incluye: nombre del socio, plan, unidad, monto y fecha de vencimiento.
- Si el socio no tiene correo registrado, se muestra mensaje de error al admin.
- No se puede reenviar la misma notificación más de una vez por día al mismo socio por la misma cuota.

---

## 2. Recordatorios automáticos

Job diario en background (`RecordatorioBackgroundService`) que evalúa las fechas de vencimiento de todos los socios activos:

| Tipo | Momento | Mensaje |
|------|---------|---------|
| `CincoDias` | 5 días antes del vencimiento | Informativo: "Tu cuota vence pronto" |
| `UnDia` | 1 día antes del vencimiento | Urgente: "Tu cuota vence mañana" |
| `DiaVencimiento` | El día del vencimiento | Aviso: "Tu cuota venció hoy" |

### Reglas

- No se envía más de un recordatorio del mismo tipo por socio por día.
- Si el socio no tiene correo, se omite y queda registrado en el sistema.
- Solo se procesan cuotas en estado Pendiente.

---

## 3. Entidad de dominio

### Entidad `RecordatorioCuota`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `Guid` | PK |
| `CuotaId` | `Guid` (FK → Cuota) | Cuota asociada |
| `SocioId` | `Guid` (FK → Socio) | Socio destinatario |
| `TipoRecordatorio` | `TipoRecordatorio` | CincoDias / UnDia / DiaVencimiento / Manual |
| `FechaEnvio` | `DateTime` | Timestamp del envío |
| `Exitoso` | `bool` | Si el envío fue exitoso |
| `Error` | `string?` | Mensaje de error si falló |

### Enum `TipoRecordatorio`

```
CincoDias = 0
UnDia = 1
DiaVencimiento = 2
Manual = 3
```

---

## 4. API

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/cuotas/{id}/notificar` | Enviar notificación manual de cuota (admin) |

---

## 5. Deuda técnica identificada

- **SmtpClient deprecated:** tiene bugs conocidos de TLS. Migrar a MailKit.
- **BG Service no re-ejecuta tras reinicio:** si el server arranca después del horario configurado, los recordatorios del día se saltean.
- **TimeSpan.Parse sin validación:** config inválida en `HoraEjecucion` crashea la app.
- **Falta endpoint manual de recordatorios:** no hay forma de disparar el cron manualmente (`POST /api/cuotas/procesar-recordatorios`).
