using System.ComponentModel.DataAnnotations;

namespace Otium.Models.DTOs;

public class LibroCrearDTO
{
    [Required(ErrorMessage = "Escribe el título del libro.")]
    [StringLength(300, ErrorMessage = "El título no puede superar 300 caracteres.")]
    public string Titulo { get; set; } = "";

    [StringLength(200, ErrorMessage = "El autor no puede superar 200 caracteres.")]
    public string Autor { get; set; } = "";

    public Estante Estante { get; set; } = Estante.QuieroLeer;

    [StringLength(500, ErrorMessage = "La URL de la portada no puede superar 500 caracteres.")]
    public string? PortadaUrl { get; set; }

    [StringLength(100, ErrorMessage = "El identificador externo no puede superar 100 caracteres.")]
    public string? IdExterno { get; set; }
}
