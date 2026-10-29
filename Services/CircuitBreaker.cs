namespace Otium.Services;

/// <summary>
/// Interruptor de circuito para proteger las llamadas a servicios externos.
/// El estado es compartido por todos los consumidores de la misma instancia
/// (registrada como singleton en DI) y todas las transiciones están
/// sincronizadas para ser consistentes bajo concurrencia.
/// </summary>
public sealed class CircuitBreaker
{
    private enum Estado { Cerrado, Abierto, MedioAbierto }

    private readonly object _candado = new();
    private Estado _estado = Estado.Cerrado;
    private int _fallosConsecutivos;
    private DateTimeOffset _horaSiguienteIntento;
    private readonly int _umbralFallos;
    private readonly TimeSpan _tiempoEspera;

    public CircuitBreaker(int umbralFallos = 3, TimeSpan? tiempoEspera = null)
    {
        _umbralFallos = umbralFallos;
        _tiempoEspera = tiempoEspera ?? TimeSpan.FromSeconds(30);
    }

    public async Task<T> EjecutarAsync<T>(Func<CancellationToken, Task<T>> accion, CancellationToken ct = default)
    {
        lock (_candado)
        {
            if (_estado == Estado.Abierto)
            {
                if (DateTimeOffset.UtcNow < _horaSiguienteIntento)
                {
                    throw new InvalidOperationException("Servicio temporalmente no disponible (circuito abierto).");
                }

                // Se concede una única llamada de prueba.
                _estado = Estado.MedioAbierto;
            }
        }

        try
        {
            // La acción se ejecuta fuera del candado para no bloquear otras llamadas.
            var resultado = await accion(ct);
            lock (_candado)
            {
                _estado = Estado.Cerrado;
                _fallosConsecutivos = 0;
            }

            return resultado;
        }
        catch
        {
            lock (_candado)
            {
                _fallosConsecutivos++;
                if (_fallosConsecutivos >= _umbralFallos)
                {
                    _estado = Estado.Abierto;
                    _horaSiguienteIntento = DateTimeOffset.UtcNow.Add(_tiempoEspera);
                }
            }

            throw;
        }
    }
}
