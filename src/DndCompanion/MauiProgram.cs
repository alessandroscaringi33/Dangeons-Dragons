using DndCompanion.Infrastructure;
using DndCompanion.Infrastructure.Logging;
using DndCompanion.ViewModels;
using DndCompanion.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace DndCompanion;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        ConfigureLogging(builder);

        builder.Services.AddDndCompanionPersistence();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<CampaignsViewModel>();
        builder.Services.AddSingleton<CampaignsPage>();
        builder.Services.AddTransient<DocumentViewModel>();
        builder.Services.AddTransient<DocumentPage>();
        builder.Services.AddTransient<StoryViewModel>();
        builder.Services.AddTransient<StoryPage>();
        builder.Services.AddTransient<CharactersViewModel>();
        builder.Services.AddTransient<CharactersPage>();
        builder.Services.AddTransient<CharacterDetailViewModel>();
        builder.Services.AddTransient<CharacterDetailPage>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }

    private static void ConfigureLogging(MauiAppBuilder builder)
    {
        builder.Logging.SetMinimumLevel(LogLevel.Information);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var logFilePath = Path.Combine(FileSystem.AppDataDirectory, "logs", "dnd-companion.log");
        builder.Logging.AddProvider(new FileLoggerProvider(logFilePath, LogLevel.Information));
    }
}