namespace Otium.Models;

public enum EstadoAmistad
{
    Pendiente,
    Aceptada,
    Rechazada
}

public class Amistad
{
    public int Id { get; set; }
    public string SolicitanteId { get; set; } = "";
    public string ReceptorId { get; set; } = "";
    public EstadoAmistad Estado { get; set; } = EstadoAmistad.Pendiente;
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRespuesta { get; set; }
}
