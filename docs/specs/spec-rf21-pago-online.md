---
tags:
  - spec
  - stub
requerimiento: RF-21
---

# RF-21 — Pago de Cuotas Online con Mercado Pago

**Caso de uso:** [[CU-11-pago-online-mercadopago]]
**Plan:** [[plan-rf21-pago-online]] *(a generar)*
**Requerimientos:** [[GymFlow_Requerimientos_Completos]]
**Iteración:** IT-6 (planificada)
**Última actualización:** 2026-06-28
**Historial:**
- 2026-06-28 — v0 (stub): notas previas a la construcción. Fija el enfoque de tooling (MCP Server de MP) y la decisión de producto: **Checkout Pro**. El diseño técnico completo (entidades, endpoints, contrato de webhook) se redacta antes de IT-6.

> **Estado:** STUB previo a la construcción. Este documento todavía **no** contiene el diseño técnico completo. Su propósito es dejar fijadas, para quien implemente RF-21: (1) el enfoque de herramientas a usar y (2) la decisión de producto Mercado Pago (**Checkout Pro**). Las reglas de negocio y criterios de aceptación viven en el [[CU-11-pago-online-mercadopago]] y son la fuente de verdad funcional.

## Enfoque de implementación (indicación para quien desarrolle)

**Usar el MCP Server oficial de Mercado Pago durante la construcción.**

Mercado Pago ofrece un MCP Server (Model Context Protocol) que asiste la integración desde el IDE / asistente de IA: genera código adaptado al stack, consulta la documentación oficial, configura webhooks/notificaciones y ejecuta pagos de prueba controlados. Es compatible con Claude Code, Cursor, Windsurf, Cline, Claude Desktop y ChatGPT.

Indicaciones:

- Conectar el MCP Server de MP al asistente de IA antes de empezar a codear RF-21.
- Verificar disponibilidad del MCP para la cuenta/país del proyecto antes de IT-6.
- Es una **herramienta de desarrollo**, no un producto de cobro: no reemplaza Checkout Pro / Checkout API / Suscripciones ni altera las reglas del CU-11. Solo acelera escribir y probar la integración una vez decidido el producto.
- Documentación: portal de developers de Mercado Pago, sección "Integrar Mercado Pago con IA" / "MCP Server".

## Decisión de producto Mercado Pago: Checkout Pro
**Producto a usar: Checkout Pro** — preferencia de pago creada en el backend + redirección del socio a la pasarela de MP + confirmación por webhook. Coherente con el flujo ya descrito en el CU-11. No se manejan datos de tarjeta en el sistema (sin carga PCI).

Alternativas evaluadas y descartadas para RF-21:

| Opción | Por qué se descartó (para este RF) |
|--------|-----------------------------------|
| **Checkout API / Bricks** | Mejor UX (pago embebido), pero mayor esfuerzo de integración. No justificado para el alcance de IT-6. |
| **Suscripciones (Preapproval)** | Cambia el modelo de cobro puntual a recurrente. Queda como posible CU/RF futuro, fuera del alcance de RF-21. |

## Pendiente de definir en la spec completa

- Entidad/registro de pagos y auditoría (RN-33).
- Contrato del endpoint de webhook y validación de firma HMAC (RN-31).
- Idempotencia del procesamiento (referencia = id de cuota + id de pago — ver E3 del CU-11).
- Estados de cuota y transiciones (ver [[spec-rf07-gestion-cuotas]] y [[CU-03-cuotas-recordatorios.md]]).
- Email de confirmación (ver [[spec-email-confirmacion-pago]]).
- Configuración de credenciales de Mercado Pago.
