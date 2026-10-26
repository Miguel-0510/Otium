using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Otium.Tests;

internal sealed class LoggerCapturador<T> : ILogger<T>
{
    private readonly ConcurrentQueue<string> _mensajes = new();

    public string Texto => string.Join(Environment.NewLine, _mensajes);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _mensajes.Enqueue($"[{logLevel}] {formatter(state, exception)}");

        if (exception is not null)
        {
            _mensajes.Enqueue(exception.ToString());
        }
    }
}
