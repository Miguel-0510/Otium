using Otium.Models;

namespace Otium.Services;

public interface IEntradaLecturaService
{
    Task<List<EntradaLectura>> ObtenerDeLibroAsync(int libroId);
    Task<EntradaLectura> AgregarAsync(int libroId, int? paginaInicio, int? paginaFin, string resumen);
    Task EliminarAsync(EntradaLectura entrada);
}
