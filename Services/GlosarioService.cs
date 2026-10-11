using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Otium.Data;
using Otium.Models;

namespace Otium.Services;

public interface IGlosarioService
{
    Task<List<PalabraGlosario>> ObtenerDeUsuarioAsync(string usuarioId);
    Task<(bool Exito, string Mensaje, PalabraGlosario? Palabra)> BuscarYGuardarAsync(string usuarioId, string palabra, string libroTitulo);
    Task EliminarAsync(PalabraGlosario palabra);
    Task EliminarTodosDeUsuarioAsync(string usuarioId);
}

public class GlosarioService : IGlosarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _modelo;

    public GlosarioService(IDbContextFactory<ApplicationDbContext> fabrica, HttpClient http, IConfiguration configuracion)
    {
        _fabrica = fabrica;
        _http = http;
        _apiKey = configuracion["Gemini:ApiKey"] ?? "";
        _modelo = configuracion["Gemini:Modelo"] ?? "gemini-flash-lite-latest";
    }

    public async Task<List<PalabraGlosario>> ObtenerDeUsuarioAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        return await db.PalabrasGlosario
            .Where(p => p.UsuarioId == usuarioId)
            .OrderBy(p => p.Palabra)
            .ToListAsync();
    }

    public async Task<(bool Exito, string Mensaje, PalabraGlosario? Palabra)> BuscarYGuardarAsync(string usuarioId, string palabra, string libroTitulo)
    {
        var texto = (palabra ?? "").Trim();
        if (string.IsNullOrEmpty(texto))
            return (false, "Escribe una palabra.", null);

        if (string.IsNullOrEmpty(_apiKey))
            return (false, "Falta configurar la llave de Gemini en el servidor.", null);

        var contexto = string.IsNullOrWhiteSpace(libroTitulo) ? "" : $" en el contexto del libro \"{libroTitulo}\"";
        var prompt = $"Da una definicion breve (maximo dos frases) y clara en espanol de la palabra \"{texto}\"{contexto}. " +
                     "Si tiene varios significados, elige el mas comun o el que mejor encaje con el contexto dado. " +
                     "Responde solo con la definicion, sin repetir la palabra, sin comillas y sin comentarios adicionales.";

        try
        {
            var url = $"v1beta/models/{_modelo}:generateContent?key={_apiKey}";
            var cuerpo = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { maxOutputTokens = 150, temperature = 0.2 }
            };

            var respuestaHttp = await _http.PostAsJsonAsync(url, cuerpo);

            if (!respuestaHttp.IsSuccessStatusCode)
            {
                var detalle = await respuestaHttp.Content.ReadAsStringAsync();
                return (false, $"Gemini respondio con un error ({(int)respuestaHttp.StatusCode}): {detalle}", null);
            }

            var datos = await respuestaHttp.Content.ReadFromJsonAsync<RespuestaGemini>();
            var definicion = datos?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(definicion))
                return (false, "No encontre una definicion para esa palabra.", null);

            var nueva = new PalabraGlosario
            {
                UsuarioId = usuarioId,
                Palabra = texto,
                Definicion = definicion,
                LibroTitulo = libroTitulo ?? ""
            };

            await using var db = await _fabrica.CreateDbContextAsync();
            db.PalabrasGlosario.Add(nueva);
            await db.SaveChangesAsync();

            return (true, "Guardada en tu glosario.", nueva);
        }
        catch (Exception ex)
        {
            return (false, "No pude consultar la definicion: " + ex.Message, null);
        }
    }

    public async Task EliminarAsync(PalabraGlosario palabra)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        db.PalabrasGlosario.Remove(palabra);
        await db.SaveChangesAsync();
    }

    public async Task EliminarTodosDeUsuarioAsync(string usuarioId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        await db.PalabrasGlosario.Where(p => p.UsuarioId == usuarioId).ExecuteDeleteAsync();
    }
}

internal class RespuestaGemini
{
    [JsonPropertyName("candidates")]
    public List<CandidatoGemini>? Candidates { get; set; }
}

internal class CandidatoGemini
{
    [JsonPropertyName("content")]
    public ContenidoGemini? Content { get; set; }
}

internal class ContenidoGemini
{
    [JsonPropertyName("parts")]
    public List<ParteGemini>? Parts { get; set; }
}

internal class ParteGemini
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
