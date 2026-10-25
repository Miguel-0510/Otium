namespace Otium.Models.DTOs;

public class LibroPaginaDTO
{
    public List<LibroDTO> Datos { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalPaginas => TamanoPagina > 0 ? (int)Math.Ceiling((double)Total / TamanoPagina) : 0;
}
