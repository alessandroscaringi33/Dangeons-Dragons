using DndCompanion.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Tests;

public sealed class FileLoggerTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "dnd-log-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Writes_StructuredJsonLine_WithLevelCategoryAndMessage()
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, "app.log");

        using (var provider = new FileLoggerProvider(filePath))
        {
            var logger = provider.CreateLogger("DndCompanion.Test");
            logger.LogInformation("Campagna aperta: {Name}", "Calderone Omnicomprensivo");
        }

        var line = File.ReadLines(filePath).Single();

        Assert.Contains("\"Level\":\"Information\"", line);
        Assert.Contains("\"Category\":\"DndCompanion.Test\"", line);
        Assert.Contains("Campagna aperta: Calderone Omnicomprensivo", line);
    }

    [Fact]
    public void SkipsMessages_BelowConfiguredMinimumLevel()
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, "app.log");

        using (var provider = new FileLoggerProvider(filePath, minimumLevel: LogLevel.Warning))
        {
            var logger = provider.CreateLogger("DndCompanion.Test");
            logger.LogDebug("Messaggio debug da ignorare");
            logger.LogError("Errore da registrare");
        }

        var content = File.ReadAllText(filePath);

        Assert.DoesNotContain("Messaggio debug da ignorare", content);
        Assert.Contains("Errore da registrare", content);
    }

    [Fact]
    public void AppendsToExistingFile()
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, "app.log");

        using (var first = new FileLoggerProvider(filePath))
        {
            first.CreateLogger("DndCompanion.Test").LogWarning("Primo evento");
        }

        using (var second = new FileLoggerProvider(filePath))
        {
            second.CreateLogger("DndCompanion.Test").LogWarning("Secondo evento");
        }

        Assert.Equal(2, File.ReadLines(filePath).Count());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}