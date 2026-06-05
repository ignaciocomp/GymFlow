# Iteracion 4 - Plan vigente

#plan

**Spec:** [[spec-it4-inscripciones-empleados-horarios]]
**Plan complementario:** [[plan-inscripcion-por-horario]]

## Objetivo

Estabilizar inscripciones, empleados y horarios con el alcance vigente:

- Inscripcion por horario individual.
- Sin lista de espera.
- Credenciales temporales por email al crear empleados.
- Horarios admin con filtro de sede obligatorio y split simple de solapamientos.
- RF-13/RF-14 cubiertos por roles y permisos configurables desde interfaz.

## Tareas implementadas

### Backend - inscripciones

- `InscripcionClase` usa `HorarioClaseId`.
- `POST /api/inscripciones` recibe `{ horarioClaseId }`.
- Si el horario esta lleno, `InscribirSocioCommand` rechaza la operacion con mensaje de cupo no disponible.
- Se auditan inscripciones y cancelaciones.
- Se envia email de confirmacion cuando la inscripcion se confirma.
- `GetMisInscripcionesQuery` usa conteo batch por horarios para evitar N+1.

### Backend - empleados

- `CrearEmpleadoRequest` no recibe password.
- `CrearEmpleadoCommand` genera password temporal con `GeneradorPassword`.
- `EmpleadoEmailTemplates.Bienvenida` envia las credenciales temporales.
- La auditoria registra si el email fue enviado o fallo.

### Frontend

- `HorariosPortalPage` permite inscribirse por horario.
- `MisInscripcionesPage` muestra dia, hora y sala.
- `NuevoUsuarioPage` no solicita password manual.
- `CatalogoClasesPage` fue eliminado.
- `SocioLayout` no muestra tab de clases separado.

### Documentacion

- [[GymFlow_Requerimientos_Completos]] documenta CU-02 por horario y sin lista de espera.
- [[spec-inscripcion-por-horario]] documenta el cambio de modelo.
- RF-14 queda documentado como cubierto por roles/permisos configurables.

## Verificacion esperada

```powershell
cd backend
dotnet test
```

```powershell
cd frontend
npm.cmd run build
```
