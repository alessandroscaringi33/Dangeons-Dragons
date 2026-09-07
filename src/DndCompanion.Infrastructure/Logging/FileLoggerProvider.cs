using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Logging;

/// <summary>
/// Writes structured (JSON lines) log entries to a file in append mode.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minimumLevel;
    private readonly StreamWriter _writer;
    private readonly object _syncRoot = new();

    public FileLoggerProvider(string filePath, LogLevel minimumLevel = LogLevel.Information)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _minimumLevel = minimumLevel;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    internal bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    internal void Write(LogLevel level, string categoryName, string message, string? exceptionText)
    {
        lock (_syncRoot)
        {
            var entry = new
            {
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
                Level = level.ToString(),
                Category = categoryName,
                Message = message,
                Exception = exceptionText,
            };

            var line = JsonSerializer.Serialize(entry);
            _writer.WriteLine(line);
        }
    }

    public void Dispose() => _writer.Dispose();
}