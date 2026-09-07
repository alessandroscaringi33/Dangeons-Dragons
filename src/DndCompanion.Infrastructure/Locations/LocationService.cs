using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Locations;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Locations;

/// <summary>
/// Manages the locations of a campaign using the campaign's SQLite database.
/// Deleting a location unlinks any NPC or quest linked to it (SetNull FK).
/// </summary>
public sealed class LocationService : ILocationService
{
    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        ICampaignDatabaseFactory databaseFactory,
        ILogger<LocationService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LocationInfo>> ListLocationsAsync(
        string campaignFolderPath,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        IQueryable<Location> query = context.Locations.AsNoTracking();

        var term = searchTerm?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLowerInvariant();
            query = query.Where(l =>
                l.Name.ToLower().Contains(lowered) ||
                l.Description.ToLower().Contains(lowered) ||
                l.Notes.ToLower().Contains(lowered));
        }

        var locations = await query
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var questCounts = await context.Quests
            .Where(q => q.LocationId != null)
            .GroupBy(q => q.LocationId!.Value)
            .Select(g => new { LocationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LocationId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        return locations
            .Select(l => ToInfo(l, questCounts.GetValueOrDefault(l.Id)))
            .ToList();
    }

    public async Task<LocationInfo> GetLocationAsync(
        string campaignFolderPath,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var location = await context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new LocationException("Il luogo non è stato trovato.");

        var questCount = await context.Quests
            .CountAsync(q => q.LocationId == locationId, cancellationToken)
            .ConfigureAwait(false);

        return ToInfo(location, questCount);
    }

    public async Task<LocationInfo> CreateLocationAsync(
        string campaignFolderPath,
        string name,
        string description,
        string notes = "",
        CancellationToken cancellationToken = default)
    {
        var normalizedName = ValidateName(name);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new LocationException("La campagna non è stata trovata o non è inizializzata.");

        var location = new Location
        {
            CampaignId = campaign.Id,
            Name = normalizedName,
            Description = description ?? string.Empty,
            Notes = notes ?? string.Empty
        };

        context.Locations.Add(location);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created location '{Name}' in campaign {Folder}", location.Name, campaignFolderPath);
        return ToInfo(location, 0);
    }

    public async Task<LocationInfo> UpdateLocationAsync(
        string campaignFolderPath,
        Guid locationId,
        string name,
        string description,
        string notes = "",
        CancellationToken cancellationToken = default)
    {
        var normalizedName = ValidateName(name);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new LocationException("Il luogo non è stato trovato.");

        location.Name = normalizedName;
        location.Description = description ?? string.Empty;
        location.Notes = notes ?? string.Empty;

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated location '{Name}' in campaign {Folder}", location.Name, campaignFolderPath);
        return ToInfo(location, 0);
    }

    public async Task DeleteLocationAsync(
        string campaignFolderPath,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new LocationException("Il luogo non è stato trovato.");

        context.Locations.Remove(location);
        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted location '{Name}' from campaign {Folder}", location.Name, campaignFolderPath);
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new LocationException("Il nome del luogo è obbligatorio.");
        }

        return name.Trim();
    }

    private static async Task SaveAsync(CampaignDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new LocationException("Non è stato possibile salvare il luogo.", ex);
        }
    }

    private static LocationInfo ToInfo(Location l, int questCount)
    {
        return new LocationInfo
        {
            Id = l.Id,
            CampaignId = l.CampaignId,
            Name = l.Name,
            Description = l.Description,
            Notes = l.Notes,
            QuestCount = questCount
        };
    }
}