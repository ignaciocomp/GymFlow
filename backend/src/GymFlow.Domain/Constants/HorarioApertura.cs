namespace GymFlow.Domain.Constants;

/// <summary>
/// Rango de apertura del gimnasio (E2E-18 parte 2). Los horarios de clase deben caer
/// completos dentro de este rango. Si a futuro se necesita por unidad o configurable
/// por entorno, puede moverse a configuración sin cambiar los puntos de validación.
/// </summary>
public static class HorarioApertura
{
    public static readonly TimeOnly Apertura = new(7, 0);
    public static readonly TimeOnly Cierre = new(22, 0);
}
