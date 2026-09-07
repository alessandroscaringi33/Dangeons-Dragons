using DndCompanion.Core.Campaigns;
using DndCompanion.Infrastructure.Campaigns;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="CampaignService"/>. Every test uses a fresh temporary
/// root folder so the real <c>Campagne</c> location is never touched.
/// </summary>
public sealed class CampaignServiceTests
{
    private readonly CampaignDatabaseFactory _databaseFactory = new();
    private readonly CampaignDatabaseInitializer _databaseInitializer;

    public CampaignServiceTests()
    {
        _databaseInitializer = new CampaignDatabaseInitializer(
            _databaseFactory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private (CampaignService Service, string Root) CreateService()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionCampaignTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);

        var pathProvider = new DefaultCampaignsPathProvider { CampaignsFolderPath = root };
        var service = new CampaignService(
            pathProvider,
            _databaseFactory,
            _databaseInitializer,
            NullLogger<CampaignService>.Instance);

        return (service, root);
    }

    private static void WriteDummyPdf(string folder, string fileName)
    {
        File.WriteAllBytes(Path.Combine(folder, fileName), new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }

    [Fact]
    public async Task ListCampaigns_WhenRootMissing_ReturnsEmpty()
    {
        var (service, root) = CreateService();
        Directory.Delete(root);

        var campaigns = await service.ListCampaignsAsync();

        Assert.Empty(campaigns);
    }

    [Fact]
    public async Task ListCampaigns_WhenRootNotAccessible_ReturnsEmpty()
    {
        var (service, root) = CreateService();
        Directory.Delete(root);
        File.WriteAllText(root, "not a directory");

        var campaigns = await service.ListCampaignsAsync();

        Assert.Empty(campaigns);
    }

    [Fact]
    public async Task ListCampaigns_WithNoCampaigns_ReturnsEmpty()
    {
        var (service, _) = CreateService();

        var campaigns = await service.ListCampaignsAsync();

        Assert.Empty(campaigns);
    }

    [Fact]
    public async Task ListCampaigns_WithOneCampaign_ReturnsIt()
    {
        var (service, root) = CreateService();
        Directory.CreateDirectory(Path.Combine(root, "Campagna A"));

        var campaigns = await service.ListCampaignsAsync();

        var campaign = Assert.Single(campaigns);
        Assert.Equal("Campagna A", campaign.Name);
        Assert.Equal(CampaignStatus.NeedsInitialization, campaign.Status);
        Assert.False(campaign.HasDatabase);
        Assert.Empty(campaign.PdfFiles);
    }

    [Fact]
    public async Task ListCampaigns_WithMultipleCampaigns_ReturnsSorted()
    {
        var (service, root) = CreateService();
        Directory.CreateDirectory(Path.Combine(root, "Campagna B"));
        Directory.CreateDirectory(Path.Combine(root, "Campagna A"));
        Directory.CreateDirectory(Path.Combine(root, "Campagna C"));

        var campaigns = await service.ListCampaignsAsync();

        Assert.Equal(3, campaigns.Count);
        Assert.Equal(
            new[] { "Campagna A", "Campagna B", "Campagna C" },
            campaigns.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task ListCampaigns_IgnoresNonDirectoryEntries()
    {
        var (service, root) = CreateService();
        Directory.CreateDirectory(Path.Combine(root, "Campagna A"));
        File.WriteAllText(Path.Combine(root, "not-a-campaign.txt"), "ignore me");

        var campaigns = await service.ListCampaignsAsync();

        var campaign = Assert.Single(campaigns);
        Assert.Equal("Campagna A", campaign.Name);
    }

    [Fact]
    public async Task MissingDatabase_IsReported()
    {
        var (service, root) = CreateService();
        var folder = Directory.CreateDirectory(Path.Combine(root, "Campagna A")).FullName;
        WriteDummyPdf(folder, "storia.pdf");

        var campaign = Assert.Single(await service.ListCampaignsAsync());

        Assert.False(campaign.HasDatabase);
        Assert.Equal(CampaignStatus.NeedsInitialization, campaign.Status);
        Assert.False(File.Exists(campaign.DatabasePath));
    }

    [Fact]
    public async Task MissingPdf_IsReported()
    {
        var (service, root) = CreateService();
        Directory.CreateDirectory(Path.Combine(root, "Campagna A"));

        var campaign = Assert.Single(await service.ListCampaignsAsync());

        Assert.False(campaign.HasPdf);
        Assert.Empty(campaign.PdfFiles);
    }

    [Fact]
    public async Task FindPdfFiles_ReturnsOnlyPdfs_IgnoringCase()
    {
        var (service, root) = CreateService();
        var folder = Directory.CreateDirectory(Path.Combine(root, "Campagna A")).FullName;
        WriteDummyPdf(folder, "storia.PDF");
        WriteDummyPdf(folder, "avventura.pdf");
        File.WriteAllText(Path.Combine(folder, "note.txt"), "not a pdf");

        var pdfs = await service.FindPdfFilesAsync(folder);

        Assert.Equal(2, pdfs.Count);
        Assert.Contains("avventura.pdf", pdfs);
        Assert.Contains("storia.PDF", pdfs);
    }

    [Fact]
    public async Task FindPdfFiles_MissingFolder_ReturnsEmpty()
    {
        var (service, _) = CreateService();

        var pdfs = await service.FindPdfFilesAsync(Path.Combine(Path.GetTempPath(), "non-esiste"));

        Assert.Empty(pdfs);
    }

    [Fact]
    public async Task CreateCampaign_CreatesFolderDatabaseAndRecord()
    {
        var (service, root) = CreateService();

        var created = await service.CreateCampaignAsync("Campagna Nuova");

        var folder = Path.Combine(root, "Campagna Nuova");
        Assert.True(Directory.Exists(folder));
        Assert.True(File.Exists(created.DatabasePath));
        Assert.Equal(CampaignStatus.Ready, created.Status);
        Assert.Equal("Campagna Nuova", created.Name);

        await using var context = _databaseFactory.CreateContext(folder);
        var stored = await context.Campaigns.SingleAsync();
        Assert.Equal("Campagna Nuova", stored.Name);
    }

    [Fact]
    public async Task CreateCampaign_DuplicateName_Throws()
    {
        var (service, _) = CreateService();
        await service.CreateCampaignAsync("Campagna A");

        await Assert.ThrowsAsync<CampaignException>(() => service.CreateCampaignAsync("Campagna A"));
    }

    [Fact]
    public async Task CreateCampaign_InvalidName_Throws()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCampaignAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCampaignAsync("  "));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCampaignAsync("Campa:gna"));
    }

    [Fact]
    public async Task OpenCampaign_MissingDatabase_CreatesAndSeeds()
    {
        var (service, root) = CreateService();
        var folder = Directory.CreateDirectory(Path.Combine(root, "Campagna A")).FullName;

        var opened = await service.OpenCampaignAsync("Campagna A");

        Assert.True(File.Exists(opened.DatabasePath));
        Assert.Equal(CampaignStatus.Ready, opened.Status);

        await using var context = _databaseFactory.CreateContext(folder);
        var stored = await context.Campaigns.SingleAsync();
        Assert.Equal("Campagna A", stored.Name);
    }

    [Fact]
    public async Task OpenCampaign_ExistingDatabase_SeedsOnlyOnce()
    {
        var (service, root) = CreateService();
        await service.CreateCampaignAsync("Campagna A");

        await service.OpenCampaignAsync("Campagna A");

        var folder = Path.Combine(root, "Campagna A");
        await using var context = _databaseFactory.CreateContext(folder);
        Assert.Equal(1, await context.Campaigns.CountAsync());
    }

    [Fact]
    public async Task OpenCampaign_Nonexistent_Throws()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<CampaignException>(() => service.OpenCampaignAsync("Non Esiste"));
    }

    [Fact]
    public async Task CreateThenOpen_PersistsAcrossContexts()
    {
        var (service, root) = CreateService();
        var created = await service.CreateCampaignAsync("Campagna Persistente");

        await service.OpenCampaignAsync("Campagna Persistente");

        var folder = Path.Combine(root, "Campagna Persistente");
        await using var context = _databaseFactory.CreateContext(folder);
        var stored = await context.Campaigns.SingleAsync(c => c.Name == "Campagna Persistente");
        Assert.Equal(created.DatabasePath, _databaseFactory.GetDatabaseFilePath(folder));
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task Listing_AfterOpen_ReportsReadyAndPdf()
    {
        var (service, root) = CreateService();
        var folder = Directory.CreateDirectory(Path.Combine(root, "Campagna A")).FullName;
        WriteDummyPdf(folder, "storia.pdf");

        await service.OpenCampaignAsync("Campagna A");

        var campaign = Assert.Single(await service.ListCampaignsAsync());
        Assert.True(campaign.HasDatabase);
        Assert.Equal(CampaignStatus.Ready, campaign.Status);
        Assert.Single(campaign.PdfFiles);
    }
}