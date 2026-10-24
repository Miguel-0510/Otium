namespace Otium.Models;

public class Bloqueo
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = "";
    public string BloqueadoId { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
