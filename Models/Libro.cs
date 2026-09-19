namespace Otium.Models;

public enum Estante
{
    Leyendo,
    Leido,
    QuieroLeer
}

public class Libro
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public Estante Estante { get; set; }
    public int Puntuacion { get; set; }   // 0 = sin puntuar, de 1 a 5
    public string Resena { get; set; } = "";
}