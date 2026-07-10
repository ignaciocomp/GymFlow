```plantuml
@startuml diagrama-clases-rf18-dashboard
' RF-18 / CU-10: dashboard en tiempo real.
' Variante del patrón Observer (publicador/suscriptor) sobre Server-Sent Events:
' no hay lista de observadores en memoria; cada suscriptor abre su propia conexión
' HTTP persistente y el publicador le empuja el estado observado cuando cambia.
' Las flechas están numeradas con los pasos del flujo (1 a 5).

set separator none
skinparam shadowing false
skinparam classAttributeIconSize 0
skinparam defaultFontName Segoe UI
skinparam defaultFontSize 12
skinparam ArrowColor #444444
skinparam ClassBorderColor #666666
skinparam ClassBackgroundColor #FDFDFD
skinparam PackageBorderColor #999999
skinparam NoteBackgroundColor #F5F5F0
skinparam NoteBorderColor #AAAAAA
skinparam PackageStyle rectangle
hide empty members

package "Frontend (React)" {

    class DashboardPage <<observador de estado>> {
        + DashboardPage() : JSX.Element
    }

    class useDashboardStream <<hook>> <<suscriptor>> {
        + useDashboardStream(unidadId? : string) : DashboardStreamState
    }

    interface DashboardStreamState {
        + data : DashboardDto | null
        + live : boolean
        + actualizadoEn : Date | null
    }

    class dashboardApi <<servicio>> {
        + get(unidadId? : string) : Promise<DashboardDto>
    }
}

package "Backend (GymFlow.API / Application)" {

    class DashboardController <<publicador>> {
        - {static} IntervaloStream : TimeSpan = 10s
        + Get(unidadId : Guid?) : Task<ActionResult<DashboardDto>>
        + Stream(unidadId : Guid?) : Task<IActionResult>
    }

    interface IUnidadesVisiblesResolver {
        + ResolverAsync(userId : Guid, rolId : Guid) : Task<IReadOnlyCollection<Guid>?>
    }

    class GetDashboardQuery {
        + ExecuteAsync(unidadId : Guid?, unidadesPermitidas : IReadOnlyCollection<Guid>?) : Task<DashboardDto>
    }

    class DashboardSnapshotDiff <<static>> {
        + {static} HaCambiado(jsonAnterior : string?, jsonActual : string) : bool
    }

    class DashboardDto <<record>> <<estado observado>> {
        + GeneradoEn : DateTime
        + SociosActivos, Cuotas, ClasesDelDia, ...
    }
}

' ── Flujo (los números son el orden del proceso) ──────────────────────────

DashboardPage --> useDashboardStream : usa el hook y se re-renderiza\ncon cada estado nuevo (4)

useDashboardStream --> dashboardApi : (1) snapshot inicial\n(5) polling de fallback cada 15s
dashboardApi ..> DashboardController : GET /api/dashboard

useDashboardStream ..> DashboardController : (2) abre el stream SSE\nGET /api/dashboard/stream

useDashboardStream ..> DashboardStreamState : (4) expone data,\nlive y actualizadoEn

DashboardController --> IUnidadesVisiblesResolver : (2) valida permiso y sedes\nvisibles antes de emitir
DashboardController --> GetDashboardQuery : (3) recalcula el snapshot\ncada ~10s
DashboardController ..> DashboardSnapshotDiff : (3) emite solo si cambió;\nsi no, heartbeat ": ping"
GetDashboardQuery ..> DashboardDto : construye

@enduml
