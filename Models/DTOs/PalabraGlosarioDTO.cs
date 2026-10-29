namespace Otium.Models.DTOs;

using System.Linq.Expressions;
using Otium.Models;

/// <summary>
/// Proyección de la palabra del glosario hacia la capa de presentación.
/// Sustituye a la entidad <see cref="PalabraGlosario"/> en la frontera
/// servicio → UI, que nunca debe ver objetos de persistencia.
/// </summary>
public class PalabraGlosarioDTO
{
    public int Id { get; set; }
    public string Palabra { get; set; } = "";
    public string Definicion { get; set; } = "";
    public string LibroTitulo { get; set; } = "";
    public DateTime Fecha { get; set; }

    /// <summary>Proyección executable por EF Core (fila → DTO dentro de la propia consulta).</summary>
    public static readonly Expression<Func<PalabraGlosario, PalabraGlosarioDTO>> Proyeccion = e => new()
    {
        Id = e.Id,
        Palabra = e.Palabra,
        Definicion = e.Definicion,
        LibroTitulo = e.LibroTitulo,
        Fecha = e.Fecha
    };

    public static PalabraGlosarioDTO DesdeEntidad(PalabraGlosario entidad) => new()
    {
        Id = entidad.Id,
        Palabra = entidad.Palabra,
        Definicion = entidad.Definicion,
        LibroTitulo = entidad.LibroTitulo,
        Fecha = entidad.Fecha
    };
}
