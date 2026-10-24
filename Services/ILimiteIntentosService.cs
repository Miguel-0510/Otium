namespace Otium.Services;

public interface ILimiteIntentosService
{
    bool Intentar(string clave, int maximo, TimeSpan ventana);
    bool IntentarCuotaDiaria(string clave, int maximo);
}
