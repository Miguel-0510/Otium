using Otium.Data;

namespace Otium.Services;

public sealed record DatosCuentaExportados(
    string Id,
    string? Correo,
    string? Usuario,
    bool CorreoConfirmado,
    string? Telefono,
    bool TelefonoConfirmado,
    bool DosFactores,
    DateTimeOffset? BloqueoHasta,
    bool ComparteEstantes);

public sealed record LibroExportado(
    int Id,
    string Titulo,
    string Autor,
    string Estante,
    int Puntuacion,
    string Resena,
    string PortadaUrl,
    string IdExterno);

public sealed record TravesiaExportada(
    int Id,
    int LibroId,
    string LibroTitulo,
    int Numero,
    int? PaginaInicio,
    int? PaginaFin,
    string Resumen,
    DateTime Fecha);

public sealed record PalabraGlosarioExportada(
    string Palabra,
    string Definicion,
    string LibroTitulo,
    DateTime Fecha);

public sealed record AmistadExportada(
    string CorreoOtroUsuario,
    string Estado,
    DateTime FechaSolicitud,
    DateTime? FechaRespuesta);

public sealed record BloqueoExportado(
    string CorreoBloqueado,
    DateTime Fecha);

public sealed record ExportacionDatos(
    DatosCuentaExportados Cuenta,
    IReadOnlyList<LibroExportado> Libros,
    IReadOnlyList<TravesiaExportada> Travesias,
    IReadOnlyList<PalabraGlosarioExportada> Glosario,
    IReadOnlyList<AmistadExportada> Amistades,
    IReadOnlyList<BloqueoExportado> Bloqueos);

public interface IExportacionDatosService
{
    Task<ExportacionDatos> ObtenerAsync(ApplicationUser usuario, CancellationToken cancellationToken = default);
}
