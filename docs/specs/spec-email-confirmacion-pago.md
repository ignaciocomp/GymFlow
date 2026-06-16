---
tags:
  - spec
requerimiento: RF-06, RF-07
---

# Email de Confirmación de Pago de Cuota + Activación SMTP

**Requerimientos:** [[GymFlow_Requerimientos_Completos]] — RF-06 (notificaciones por email), RF-07 (gestión de cuotas)
**Specs relacionadas:** [[spec-rf06-recordatorios-cuota]], [[spec-rf07-gestion-cuotas]]
**Última actualización:** 2026-06-16
**Estado:** Implementado (PR #29)

## Resumen

Extiende el sistema de emails de cuotas con un **aviso de confirmación de pago**: cuando el admin marca una cuota como pagada, el socio recibe un email confirmándolo (plan, sede, monto, período y fecha de pago). Hasta ahora el sistema solo enviaba recordatorios de vencimiento (RF-06); no había ningún aviso al registrarse el pago.

Además define el mecanismo para **activar el envío real de emails en producción** (Azure Container Apps) sin que ninguna credencial toque el repositorio.

## Decisiones de diseño

### Email de confirmación de pago

- **Disparador:** `MarcarCuotaPagadaCommand`, **después** de persistir el pago. El email nunca bloquea ni revierte la operación.
- **Best-effort reforzado:** el envío (resolución del socio + armado de plantilla + `EnviarAsync`) va envuelto en `try/catch`. Si el SMTP devuelve `Exitoso=false` **o lanza una excepción**, el pago igual queda confirmado. Esto es estrictamente más robusto que el patrón previo (que solo confiaba en `EmailResultado`).
- **Resolución del socio:** se usa la navegación `cuota.Socio` si vino cargada; si no, se cae al repositorio por `SocioId`. Si el socio no existe o no tiene correo, se omite el envío sin fallar.
- **Plantilla:** método `ConfirmacionPago(socio, cuota)` agregado a la clase `EmailTemplates` existente de cuotas (la misma que arma los recordatorios), respetando el estilo del resto: devuelve `(Asunto, Cuerpo)` y aplica `WebUtility.HtmlEncode` a **todo** valor dinámico (nombre, plan, sede).
- **Contenido:** asunto "Pago confirmado: tu cuota de {plan}"; cuerpo con plan, sede (fila condicional si la unidad está cargada), monto, período (mes/año de emisión) y fecha de pago.

### Activación de SMTP en producción

- **`SmtpEmailService`** lee de configuración: `Email:Habilitado`, `Email:SmtpHost`, `Email:SmtpPort`, `Email:SmtpUser`, `Email:SmtpPassword`, `Email:From`, `Email:EnableSsl`. En `appsettings.json` queda `Habilitado=false` con host `smtp.gmail.com:587` precargado; en dev local los emails se **simulan** (solo log), intencionalmente, para no enviar correos al probar.
- **Workflow manual `configure-email.yml`** (`workflow_dispatch`): valida que existan los secrets `SMTP_USER`/`SMTP_PASSWORD`, hace `az login`, guarda la App Password como **secret del Container App** (`smtp-password`) y setea las env vars `Email__Habilitado=true`, `Email__SmtpUser`, `Email__From` y `Email__SmtpPassword=secretref:smtp-password`.
- **Sin credenciales en el repo:** la App Password vive solo como secret de GitHub y de Azure; nunca se imprime en logs (`--output none`, sin `echo`). Es one-time: las env vars persisten entre deploys de imagen.
- **Remitente:** Gmail fuerza el `From` a la cuenta autenticada; la cuenta requiere verificación en 2 pasos y una App Password de 16 caracteres (sin espacios). Pasos manuales documentados en `docs/deploy/SETUP-CICD.md`.

## Criterios de aceptación

- Al marcar una cuota como pagada, el socio con correo recibe un email de confirmación con los datos de la cuota.
- Si el envío del email falla (SMTP caído o excepción), el pago igual queda registrado y la operación responde OK.
- Si la cuota no tiene socio asociado o el socio no tiene correo, no se envía email y no se produce error.
- Todo valor dinámico de la plantilla se escapa con `HtmlEncode`.
- En dev local (`Email:Habilitado=false`) los emails se simulan (log), no se envían.
- Corriendo el workflow "Configurar email" con los secrets cargados, el envío real queda activo en producción sin exponer la contraseña.

## Fuera de alcance

- Email de aviso al **anular** o **revertir** un pago (hoy solo se notifica el pago confirmado).
- Migración de `SmtpClient` (deprecado) a MailKit — deuda técnica preexistente.
- Personalización de plantillas por unidad/marca.
