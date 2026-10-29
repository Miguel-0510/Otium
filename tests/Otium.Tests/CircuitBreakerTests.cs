using Otium.Services;

namespace Otium.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task Veinte_fallos_paralelos_abren_el_circuito_una_sola_vez()
    {
        var breaker = new CircuitBreaker(umbralFallos: 3, tiempoEspera: TimeSpan.FromMinutes(1));

        var desenlaces = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            Task.Run(async () =>
            {
                try
                {
                    await breaker.EjecutarAsync<int>((_) => ExcepcionPropia());
                    return "sin-error";
                }
                catch (InvalidOperationException)
                {
                    return "circuito-abierto";
                }
                catch (ExcepcionDePrueba)
                {
                    return "fallo-propio";
                }
            })));

        // El circuito debió abrirse en algún intento y, desde ahí, fallar en seco.
        Assert.Equal(20, desenlaces.Length);
        Assert.Contains("circuito-abierto", desenlaces);
        Assert.Contains("fallo-propio", desenlaces);

        // Estado Abierto persistente: una llamada nueva ni siquiera ejecuta la acción.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.EjecutarAsync<int>((_) => Task.FromResult(0)));
    }

    [Fact]
    public async Task El_exito_reinicia_el_contador_de_fallos()
    {
        var breaker = new CircuitBreaker(umbralFallos: 3, tiempoEspera: TimeSpan.FromSeconds(30));

        // Dos fallos (por debajo del umbral) + un éxito (resetea) + dos fallos más.
        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<ExcepcionDePrueba>(() => breaker.EjecutarAsync<int>((_) => ExcepcionPropia()));
        }

        Assert.Equal(7, await breaker.EjecutarAsync((_) => Task.FromResult(7)));

        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<ExcepcionDePrueba>(() => breaker.EjecutarAsync<int>((_) => ExcepcionPropia()));
        }

        // Si el contador no se hubiera reiniciado, este éxito habría ido a medio-abierto tras abrirse.
        Assert.Equal(9, await breaker.EjecutarAsync((_) => Task.FromResult(9)));
    }

    [Fact]
    public async Task Vencido_el_tiempo_de_espera_concede_una_llamada_de_prueba()
    {
        var breaker = new CircuitBreaker(umbralFallos: 1, tiempoEspera: TimeSpan.FromMilliseconds(60));

        // Un fallo abre el circuito al instante (umbral 1).
        await Assert.ThrowsAsync<ExcepcionDePrueba>(() => breaker.EjecutarAsync<int>((_) => ExcepcionPropia()));

        // Antes de vencer el tiempo, sigue abierto.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.EjecutarAsync<int>((_) => Task.FromResult(1)));

        await Task.Delay(90);

        // Pasado el tiempo, medio-abierto permite la llamada de prueba; si tiene éxito, se cierra.
        Assert.Equal(2, await breaker.EjecutarAsync((_) => Task.FromResult(2)));

        // Cerrado con umbral 1: un solo fallo vuelve a abrir el circuito.
        await Assert.ThrowsAsync<ExcepcionDePrueba>(() => breaker.EjecutarAsync<int>((_) => ExcepcionPropia()));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.EjecutarAsync<int>((_) => Task.FromResult(3)));
    }

    private static Task<int> ExcepcionPropia() => throw new ExcepcionDePrueba("fallo simulado");

    private sealed class ExcepcionDePrueba(string mensaje) : Exception(mensaje);
}
