namespace Otium.Services;

public sealed class CircuitBreaker
{
    private enum Estado { Cerrado, Abierto, MedioAbierto }

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
        if (_estado == Estado.Abierto)
        {
            if (DateTimeOffset.UtcNow < _horaSiguienteIntento)
                throw new InvalidOperationException("Servicio temporalmente no disponible (circuito abierto).");
            _estado = Estado.MedioAbierto;
        }

        try
        {
            var resultado = await accion(ct);
            _estado = Estado.Cerrado;
            _fallosConsecutivos = 0;
            return resultado;
        }
        catch
        {
            _fallosConsecutivos++;
            if (_fallosConsecutivos >= _umbralFallos)
            {
                _estado = Estado.Abierto;
                _horaSiguienteIntento = DateTimeOffset.UtcNow.Add(_tiempoEspera);
            }
            throw;
        }
    }
}
