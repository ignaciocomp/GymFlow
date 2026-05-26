# Diseño — Mejoras RF-01: Tipo de Documento y Validación de Cédula Uruguaya

**Fecha:** 2026-03-28
**Estado:** Aprobado
**Alcance:** Backend únicamente (Domain, Application, Infrastructure)

---

## Contexto

El RF-01 (Alta de Socio) maneja el campo `DocumentoIdentidad` como un `string?` libre sin distinción de tipo. Se requiere:

1. Agregar un enum `TipoDocumento` con valores `CI`, `Pasaporte` y `Otro`.
2. Validar que el documento sea una cédula uruguaya válida cuando `TipoDocumento == CI`.
3. El campo es obligatorio al crear y actualizar un socio.
4. La validación aplica tanto en create como en update.

---

## Decisiones de Diseño

- **Enfoque elegido: validación en Domain.** La validación de cédula uruguaya es una regla de negocio pura, consistente con el patrón ya establecido en `Socio` (validación de `consentimientoInformado`). No se necesita un servicio externo ni inyección de dependencias.
- **`TipoDocumento` es requerido** — no puede registrarse un socio sin especificarlo.
- **`DocumentoIdentidad` sigue siendo `string?`** — el número de documento sigue siendo opcional para tipos `Pasaporte` y `Otro`, pero es obligatorio cuando `TipoDocumento == CI`.
- **Default en migración: `Otro` (valor 2)** — los registros existentes no pueden asumir formato de cédula válida, por lo que se asigna `Otro` para mantener consistencia.

---

## Sección 1 — Modelo de Datos (Domain)

### Nuevo enum `TipoDocumento`
**Archivo:** `backend/src/GymFlow.Domain/Enums/TipoDocumento.cs`

```
CI = 0
Pasaporte = 1
Otro = 2
```

### Cambios en entidad `Socio`
**Archivo:** `backend/src/GymFlow.Domain/Entities/Socio.cs`

- Nueva propiedad: `TipoDocumento TipoDocumento` (sin nullable, requerida)
- Constructor: recibe `TipoDocumento tipoDocumento` como parámetro obligatorio
- Método `ActualizarDatosSocio`: recibe `TipoDocumento tipoDocumento` como parámetro obligatorio
- Método privado estático `EsCedulaUruguayaValida(string doc)`: implementa el algoritmo de dígito verificador uruguayo
  - Normaliza el input: elimina puntos y guiones
  - Acepta 7 u 8 dígitos
  - Aplica los pesos `[2, 9, 8, 7, 6, 3, 4]` sobre los primeros 7 dígitos (paddeando a 8 con cero a la izquierda si tiene 7)
  - Verifica que `(suma + dígito_verificador) % 10 == 0`
- Regla: si `TipoDocumento == CI`, se valida que `DocumentoIdentidad` no sea null/vacío y que sea una cédula válida. Si no cumple, se lanza `ArgumentException`.

---

## Sección 2 — Capa Application y DTOs

### DTOs modificados
**`CreateSocioRequest`:** agregar `TipoDocumento TipoDocumento` (requerido)
**`UpdateSocioRequest`:** agregar `TipoDocumento TipoDocumento` (requerido)
**`SocioDto`:** agregar `TipoDocumento TipoDocumento` para exponerlo en las respuestas

### Use Cases modificados
**`CreateSocioCommand`:** pasar `request.TipoDocumento` al constructor de `Socio`. Sin lógica de validación propia — la validación queda en Domain.
**`UpdateSocioCommand`:** pasar `request.TipoDocumento` a `ActualizarDatosSocio`. Sin lógica de validación propia.

---

## Sección 3 — Infraestructura

### EF Core
- **Nueva migración:** agrega columna `TipoDocumento` (int, NOT NULL) a la tabla `Usuarios` (TPH).
- **Default para registros existentes:** `2` (`Otro`) — evita inconsistencias con documentos previos que no tienen formato de cédula validado.
- La migración se aplica automáticamente en el próximo `docker compose up`.

---

## Sección 4 — Tests

### Nuevos: `GymFlow.Domain.Tests/Entities/SocioTests.cs`
Tests del método `EsCedulaUruguayaValida` y del constructor:
- Cédula válida con 8 dígitos → no lanza excepción
- Cédula válida con 7 dígitos (paddeo) → no lanza excepción
- Cédula válida con puntos y guiones (ej. `1.234.567-8`) → no lanza excepción
- Cédula con dígito verificador incorrecto → lanza `ArgumentException`
- String no numérico → lanza `ArgumentException`
- `DocumentoIdentidad` null con `TipoDocumento == CI` → lanza `ArgumentException`
- `TipoDocumento == Pasaporte` con documento inválido como CI → no lanza excepción
- `TipoDocumento == Otro` sin documento → no lanza excepción

### Nuevos: `GymFlow.Application.Tests/UseCases/CreateSocioCommandTests.cs`
- Crear socio con `TipoDocumento == CI` y cédula válida → retorna `SocioDto` con `TipoDocumento` correcto
- Crear socio con `TipoDocumento == CI` y cédula inválida → lanza excepción (propagada desde Domain)
- Crear socio con `TipoDocumento == Pasaporte` → retorna `SocioDto` correctamente
- Crear socio con `TipoDocumento == Otro` sin documento → retorna `SocioDto` correctamente

---

## Archivos Afectados

| Archivo | Cambio |
|---------|--------|
| `Domain/Enums/TipoDocumento.cs` | Nuevo |
| `Domain/Entities/Socio.cs` | Modificado |
| `Application/DTOs/CreateSocioRequest.cs` | Modificado |
| `Application/DTOs/UpdateSocioRequest.cs` | Modificado |
| `Application/DTOs/SocioDto.cs` | Modificado |
| `Application/UseCases/Socios/CreateSocioCommand.cs` | Modificado |
| `Application/UseCases/Socios/UpdateSocioCommand.cs` | Modificado |
| `Infrastructure/Persistence/Migrations/` | Nueva migración |
| `Domain.Tests/Entities/SocioTests.cs` | Nuevo |
| `Application.Tests/UseCases/CreateSocioCommandTests.cs` | Nuevo |
