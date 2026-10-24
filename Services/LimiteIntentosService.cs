using Microsoft.Extensions.Caching.Memory;

namespace Otium.Services;

public sealed class LimiteIntentosService : ILimiteIntentosService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LimiteIntentosService> _logger;

    public LimiteIntentosService(IMemoryCache cache, ILogger<LimiteIntentosService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public bool Intentar(string clave, int maximo, TimeSpan ventana)
    {
        var contador = _cache.GetOrCreate(clave, entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = ventana;
            return new Contador(DateTimeOffset.UtcNow);
        })!;

        lock (contador)
        {
            var ahora = DateTimeOffset.UtcNow;
            if (ahora - contador.Inicio >= ventana)
            {
                contador.Inicio = ahora;
                contador.Cuenta = 0;
            }

            if (contador.Cuenta >= maximo)
            {
                _logger.LogWarning("Límite de intentos alcanzado para {Clave}.", clave);
                return false;
            }

            contador.Cuenta++;
            return true;
        }
    }

    public bool IntentarCuotaDiaria(string clave, int maximo) =>
        Intentar($"{clave}:{DateTime.UtcNow:yyyyMMdd}", maximo, TimeSpan.FromDays(1));

    private sealed class Contador
    {
        public Contador(DateTimeOffset inicio)
        {
            Inicio = inicio;
        }

        public DateTimeOffset Inicio { get; set; }
        public int Cuenta { get; set; }
    }
}
