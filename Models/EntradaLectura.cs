using System.ComponentModel.DataAnnotations;

namespace Otium.Models;

public class EntradaLectura
{
    public int Id { get; set; }
    public int LibroId { get; set; }
    public int Numero { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "La página inicial no puede ser negativa.")]
    public int? PaginaInicio { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "La página final no puede ser negativa.")]
    public int? PaginaFin { get; set; }

    [StringLength(2000, ErrorMessage = "El resumen no puede superar 2000 caracteres.")]
    public string Resumen { get; set; } = "";

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
