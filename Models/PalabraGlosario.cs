using System.ComponentModel.DataAnnotations;

namespace Otium.Models;

public class PalabraGlosario
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = "";

    [Required(ErrorMessage = "Escribe una palabra.")]
    [StringLength(100, ErrorMessage = "La palabra no puede superar 100 caracteres.")]
    public string Palabra { get; set; } = "";

    [StringLength(1000, ErrorMessage = "La definición no puede superar 1000 caracteres.")]
    public string Definicion { get; set; } = "";

    [StringLength(300, ErrorMessage = "El título del libro no puede superar 300 caracteres.")]
    public string LibroTitulo { get; set; } = "";

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
