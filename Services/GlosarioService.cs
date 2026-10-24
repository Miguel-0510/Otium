using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Otium.Data;
using Otium.Models;
using Otium.Services.Options;

namespace Otium.Services;

public interface IGlosarioService
{
    Task<List<PalabraGlosario>> ObtenerDeUsuarioAsync(string usuarioId);
    Task<(bool Exito, string Mensaje, PalabraGlosario? Palabra)> BuscarYGuardarAsync(string usuarioId, string palabra, string libroTitulo);
    Task<bool> EliminarAsync(string usuarioId, int palabraId);
    Task EliminarTodosDeUsuarioAsync(string usuarioId);
}

public class GlosarioService : IGlosarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _fabrica;
    private readonly HttpClient _http;
    private readonly ILogger<GlosarioService> _logger;
    private readonly ILimiteIntentosService _limites;
    private readonly string _apiKey;
    private readonly string _modelo;

    public GlosarioService(
        IDbContextFactory<ApplicationDbContext> fabrica,
        HttpClient http,
        IOptions<GeminiOptions> opciones,
        ILimiteIntentosService limites,
        ILogger<GlosarioService> logger)
    {
        _fabrica = fabrica;
        _http = http;
        _logger = logger;
        _limites = limites;
        _apiKey = opciones.Value.ApiKey;
        _modelo = opciones.Value.Modelo;
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

        if (!_limites.IntentarCuotaDiaria($"glosario:{usuarioId}", 50))
            return (false, "Alcanzaste el límite diario de definiciones. Inténtalo mañana.", null);

        var contexto = string.IsNullOrWhiteSpace(libroTitulo) ? "" : $" en el contexto del libro \"{libroTitulo}\"";
        var prompt = $"Da una definicion breve (maximo dos frases) y clara en espanol de la palabra \"{texto}\"{contexto}. " +
                     "Si tiene varios significados, elige el mas comun o el que mejor encaje con el contexto dado. " +
                     "Responde solo con la definicion, sin repetir la palabra, sin comillas y sin comentarios adicionales.";

        try
        {
            var url = $"v1beta/models/{_modelo}:generateContent";
            var cuerpo = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { maxOutputTokens = 150, temperature = 0.2 }
            };

            using var solicitud = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(cuerpo)
            };
            solicitud.Headers.Add("x-goog-api-key", _apiKey);

            using var respuestaHttp = await _http.SendAsync(solicitud);

            if (!respuestaHttp.IsSuccessStatusCode)
            {
                var detalle = await respuestaHttp.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Gemini respondió {Estado} al definir una palabra. Detalle: {Detalle}",
                    (int)respuestaHttp.StatusCode,
                    detalle);

                return (false, "No pude consultar la definición en este momento. Inténtalo más tarde.", null);
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
            _logger.LogError(ex, "Error al consultar Gemini para el glosario.");
            return (false, "No pude consultar la definición en este momento. Inténtalo más tarde.", null);
        }
    }

    public async Task<bool> EliminarAsync(string usuarioId, int palabraId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var filas = await db.PalabrasGlosario
            .Where(p => p.Id == palabraId && p.UsuarioId == usuarioId)
            .ExecuteDeleteAsync();

        return filas > 0;
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
