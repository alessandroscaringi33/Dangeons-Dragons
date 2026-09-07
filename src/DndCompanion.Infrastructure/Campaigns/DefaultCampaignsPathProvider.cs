using DndCompanion.Core.Campaigns;

namespace DndCompanion.Infrastructure.Campaigns;

/// <summary>
/// Default <see cref="ICampaignsPathProvider"/>. The location is resolved in
/// this order: the <c>DND_COMPANION_CAMPAIGNS_PATH</c> environment variable, a
/// <c>Campagne</c> folder found while walking up from the current directory,
/// then a <c>Campagne</c> folder next to the application. It can be overridden
/// at any time (e.g. from a settings screen).
/// </summary>
public sealed class DefaultCampaignsPathProvider : ICampaignsPathProvider
{
    private string _campaignsFolderPath;

    public DefaultCampaignsPathProvider()
    {
        _campaignsFolderPath = ResolveDefaultPath();
    }

    public string CampaignsFolderPath
    {
        get => _campaignsFolderPath;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _campaignsFolderPath = value;
        }
    }

    private static string ResolveDefaultPath()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("DND_COMPANION_CAMPAIGNS_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Campagne");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Campagne");
    }
}