using System.Security.Cryptography;

namespace Otium.Services;

public sealed class CodigoAmigoGenerator : ICodigoAmigoGenerator
{
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Longitud = 8;

    public string Generar()
    {
        Span<char> buffer = stackalloc char[Longitud];

        for (var i = 0; i < Longitud; i++)
        {
            buffer[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        }

        return new string(buffer);
    }
}
