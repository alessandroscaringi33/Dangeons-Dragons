using DndCompanion.Core.Campaigns;
using DndCompanion.Core.Characters;
using DndCompanion.Core.Dice;
using DndCompanion.Core.Documents;
using DndCompanion.Core.Locations;
using DndCompanion.Core.Npcs;
using DndCompanion.Core.Quests;
using DndCompanion.Core.Story;
using DndCompanion.Infrastructure.Campaigns;
using DndCompanion.Infrastructure.Characters;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Documents;
using DndCompanion.Infrastructure.Locations;
using DndCompanion.Infrastructure.Npcs;
using DndCompanion.Infrastructure.Quests;
using DndCompanion.Infrastructure.Story;
using Microsoft.Extensions.DependencyInjection;

namespace DndCompanion.Infrastructure;

/// <summary>
/// Registers the persistence, campaign management and document services into
/// the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDndCompanionPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ICampaignsPathProvider, DefaultCampaignsPathProvider>();
        services.AddSingleton<ICampaignDatabaseFactory, CampaignDatabaseFactory>();
        services.AddSingleton<ICampaignDatabaseInitializer, CampaignDatabaseInitializer>();
        services.AddSingleton<ICampaignService, CampaignService>();
        services.AddSingleton<IPdfReader, PdfPigPdfReader>();
        services.AddSingleton<IChapterService, ChapterService>();
        services.AddSingleton<ISceneService, SceneService>();
        services.AddSingleton<ICharacterService, CharacterService>();
        services.AddSingleton<INpcService, NpcService>();
        services.AddSingleton<ILocationService, LocationService>();
        services.AddSingleton<IQuestService, QuestService>();
        services.AddSingleton<IDiceService, DiceService>();
        return services;
    }
}