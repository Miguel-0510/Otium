namespace Otium.Models;

public class PalabraGlosario
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = "";
    public string Palabra { get; set; } = "";
    public string Definicion { get; set; } = "";
    public string LibroTitulo { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
