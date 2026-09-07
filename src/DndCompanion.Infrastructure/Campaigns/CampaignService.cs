using DndCompanion.Core.Campaigns;
using DndCompanion.Core.Domain.Entities;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Campaigns;

/// <summary>
/// Manages campaign folders under the configured <c>Campagne</c> root:
/// discovery, creation, opening and database initialization.
/// PDF files are only ever detected and never modified or deleted.
/// </summary>
public sealed class CampaignService : ICampaignService
{
    private readonly ICampaignsPathProvider _pathProvider;
    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly ICampaignDatabaseInitializer _databaseInitializer;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(
        ICampaignsPathProvider pathProvider,
        ICampaignDatabaseFactory databaseFactory,
        ICampaignDatabaseInitializer databaseInitializer,
        ILogger<CampaignService> logger)
    {
        _pathProvider = pathProvider;
        _databaseFactory = databaseFactory;
        _databaseInitializer = databaseInitializer;
        _logger = logger;
    }

    public string CampaignsFolderPath => _pathProvider.CampaignsFolderPath;

    public Task<IReadOnlyList<CampaignInfo>> ListCampaignsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = _pathProvider.CampaignsFolderPath;

        if (!Directory.Exists(root))
        {
            _logger.LogInformation("Campaigns folder does not exist: {Root}", root);
            return Task.FromResult<IReadOnlyList<CampaignInfo>>(Array.Empty<CampaignInfo>());
        }

        string[] directories;
        try
        {
            directories = Directory.GetDirectories(root);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Campaigns folder is not accessible: {Root}", root);
            return Task.FromResult<IReadOnlyList<CampaignInfo>>(Array.Empty<CampaignInfo>());
        }

        var campaigns = new List<CampaignInfo>(directories.Length);
        foreach (var directory in directories)
        {
            try
            {
                campaigns.Add(BuildCampaignInfo(directory));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping inaccessible campaign folder: {Folder}", directory);
                campaigns.Add(BuildErrorInfo(directory));
            }
        }

        return Task.FromResult<IReadOnlyList<CampaignInfo>>(
            campaigns.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public async Task<CampaignInfo> CreateCampaignAsync(string name, CancellationToken cancellationToken = default)
    {
        var campaignName = ValidateAndNormalizeName(name);
        var folder = Path.Combine(_pathProvider.CampaignsFolderPath, campaignName);

        if (Directory.Exists(folder) || File.Exists(folder))
        {
            throw new CampaignException($"Esiste già una campagna chiamata '{campaignName}'.");
        }

        try
        {
            Directory.CreateDirectory(folder);

            await _databaseInitializer.InitializeAsync(folder, cancellationToken).ConfigureAwait(false);

            await using var context = _databaseFactory.CreateContext(folder);
            context.Campaigns.Add(new Campaign { Name = campaignName });
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Campaign '{Name}' created at {Folder}", campaignName, folder);
        }
        catch (Exception ex) when (ex is not CampaignException)
        {
            _logger.LogError(ex, "Failed to create campaign '{Name}'", campaignName);
            throw new CampaignException($"Non è stato possibile creare la campagna '{campaignName}'.", ex);
        }

        return BuildCampaignInfo(folder);
    }

    public async Task<CampaignInfo> OpenCampaignAsync(string campaignFolderName, CancellationToken cancellationToken = default)
    {
        var campaignName = ValidateAndNormalizeName(campaignFolderName);
        var folder = Path.Combine(_pathProvider.CampaignsFolderPath, campaignName);

        if (!Directory.Exists(folder))
        {
            throw new CampaignException($"La campagna '{campaignName}' non è stata trovata.");
        }

        try
        {
            // Creates campaign.db when missing and applies pending migrations.
            await _databaseInitializer.InitializeAsync(folder, cancellationToken).ConfigureAwait(false);

            await using var context = _databaseFactory.CreateContext(folder);

            // Verifies the database is usable and seeds a campaign record for
            // folders that were never opened before.
            var campaign = await context.Campaigns.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (campaign is null)
            {
                context.Campaigns.Add(new Campaign { Name = campaignName });
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Campaign '{Name}' opened at {Folder}", campaignName, folder);
        }
        catch (Exception ex) when (ex is not CampaignException)
        {
            _logger.LogError(ex, "Failed to open campaign '{Name}'", campaignName);
            throw new CampaignException($"Non è stato possibile aprire la campagna '{campaignName}'.", ex);
        }

        return BuildCampaignInfo(folder);
    }

    public Task<IReadOnlyList<string>> FindPdfFilesAsync(string campaignFolderPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignFolderPath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(campaignFolderPath))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        return Task.FromResult(FindPdfFiles(campaignFolderPath));
    }

    private CampaignInfo BuildCampaignInfo(string folder)
    {
        var databasePath = _databaseFactory.GetDatabaseFilePath(folder);
        var hasDatabase = File.Exists(databasePath);

        return new CampaignInfo
        {
            Name = Path.GetFileName(folder),
            FolderPath = folder,
            DatabasePath = databasePath,
            PdfFiles = FindPdfFiles(folder),
            LastModified = Directory.GetLastWriteTimeUtc(folder),
            HasDatabase = hasDatabase,
            Status = hasDatabase ? CampaignStatus.Ready : CampaignStatus.NeedsInitialization
        };
    }

    private static CampaignInfo BuildErrorInfo(string folder)
    {
        return new CampaignInfo
        {
            Name = Path.GetFileName(folder),
            FolderPath = folder,
            Status = CampaignStatus.Error
        };
    }

    private static IReadOnlyList<string> FindPdfFiles(string folder)
    {
        return Directory.EnumerateFiles(folder, "*.pdf", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ValidateAndNormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Il nome della campagna è obbligatorio.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"Il nome '{trimmed}' contiene caratteri non validi.", nameof(name));
        }

        return trimmed;
    }
}