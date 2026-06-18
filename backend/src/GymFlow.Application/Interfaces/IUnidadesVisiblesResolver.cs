namespace GymFlow.Application.Interfaces;

/// <summary>
/// Resuelve, server-side, el conjunto de unidades que un usuario puede ver/operar a partir
/// de su <c>userId</c> y <c>rolId</c> (tomados del JWT). El backend no confía en el cliente:
/// el filtrado por unidad se decide acá, no en parámetros de la request.
/// <list type="bullet">
///   <item>Admin → <c>null</c> (sin restricción: ve todas las unidades).</item>
///   <item>Dueño → las <c>UnidadId</c> de sus <c>UsuarioUnidad</c> (puede ser vacío si no tiene asignadas).</item>
///   <item>Cualquier otro rol → <c>null</c> (su acceso lo gobiernan los permisos de módulo).</item>
/// </list>
/// </summary>
public interface IUnidadesVisiblesResolver
{
    Task<IReadOnlyCollection<Guid>?> ResolverAsync(Guid userId, Guid rolId, CancellationToken ct = default);
}
