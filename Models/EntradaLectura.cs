namespace Otium.Models;

public class EntradaLectura
{
    public int Id { get; set; }
    public int LibroId { get; set; }
    public int Numero { get; set; }
    public int? PaginaInicio { get; set; }
    public int? PaginaFin { get; set; }
    public string Resumen { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
