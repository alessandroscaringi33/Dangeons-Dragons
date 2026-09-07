using DndCompanion.Core.Campaigns;
using DndCompanion.Core.Documents;
using DndCompanion.Infrastructure.Campaigns;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Documents;
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
        return services;
    }
}