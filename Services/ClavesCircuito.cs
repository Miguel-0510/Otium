namespace Otium.Services;

/// <summary>
/// Claves de DI para los interruptores de circuito compartidos (singletons).
/// Cada cliente externo tiene el suyo para que un fallo de un proveedor
/// no abra el circuito de los demás.
/// </summary>
public static class ClavesCircuito
{
    public const string Definiciones = "circuito-definiciones";
    public const string Buscador = "circuito-buscador";
}
