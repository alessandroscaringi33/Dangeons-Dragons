using System.Reflection;
using DndCompanion.Core.Mvvm;
using DndCompanion.Infrastructure.Logging;

namespace DndCompanion.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Core_DoesNotReference_MauiOrInfrastructure()
    {
        var assembly = typeof(ViewModelBase).Assembly;
        var referencedNames = assembly
            .GetReferencedAssemblies()
            .Select(r => r.Name)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(referencedNames, n => n!.StartsWith("Microsoft.Maui", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedNames, n => n!.StartsWith("Xamarin", StringComparison.Ordinal));
        Assert.DoesNotContain("DndCompanion.Infrastructure", referencedNames);
    }

    [Fact]
    public void Infrastructure_DeclaresReferenceToCore()
    {
        var projectFile = FindProjectFile("DndCompanion.Infrastructure");
        var content = File.ReadAllText(projectFile);

        Assert.Contains(@"..\DndCompanion.Core\DndCompanion.Core.csproj", content);
    }

    [Fact]
    public void Infrastructure_References_LoggingAbstractions()
    {
        var assembly = typeof(FileLoggerProvider).Assembly;
        var referencedNames = assembly.GetReferencedAssemblies().Select(r => r.Name);

        Assert.Contains("Microsoft.Extensions.Logging.Abstractions", referencedNames);
    }

    [Fact]
    public void Tests_Reference_CoreAndInfrastructure()
    {
        var assembly = typeof(ArchitectureTests).Assembly;
        var referencedNames = assembly
            .GetReferencedAssemblies()
            .Select(r => r.Name)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("DndCompanion.Core", referencedNames);
        Assert.Contains("DndCompanion.Infrastructure", referencedNames);
    }

    private static string FindProjectFile(string projectName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, projectName, $"{projectName}.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Project '{projectName}' not found.");
    }
}