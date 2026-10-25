namespace Otium.Models.DTOs;

public class LibroDTO
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public Estante Estante { get; set; }
    public int Puntuacion { get; set; }
    public string? Resena { get; set; }
    public string? PortadaUrl { get; set; }
    public string? IdExterno { get; set; }
}
