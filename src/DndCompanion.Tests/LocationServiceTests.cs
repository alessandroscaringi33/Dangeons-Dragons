using DndCompanion.Core.Locations;
using DndCompanion.Infrastructure.Data;
using DndCompanion.Infrastructure.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

/// <summary>
/// Tests for <see cref="LocationService"/>. Every test uses a fresh temporary
/// campaign folder and database, mirroring the real <c>Campagne</c> layout.
/// </summary>
public sealed class LocationServiceTests
{
    private readonly CampaignDatabaseFactory _factory = new();
    private readonly CampaignDatabaseInitializer _initializer;

    public LocationServiceTests()
    {
        _initializer = new CampaignDatabaseInitializer(
            _factory,
            NullLogger<CampaignDatabaseInitializer>.Instance);
    }

    private async Task<string> CreateCampaignFolderAsync()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionLocationTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folder);
        await _initializer.InitializeAsync(folder);

        await using var context = _factory.CreateContext(folder);
        context.Campaigns.Add(new Core.Domain.Entities.Campaign { Name = Path.GetFileName(folder) });
        await context.SaveChangesAsync();

        return folder;
    }

    private LocationService CreateService()
    {
        return new LocationService(_factory, NullLogger<LocationService>.Instance);
    }

    [Fact]
    public async Task Create_PersistsFields()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var created = await service.CreateLocationAsync(folder, "Taverna del Drago", "Un locale fumoso");

        Assert.Equal("Taverna del Drago", created.Name);
        Assert.Equal("Un locale fumoso", created.Description);
    }

    [Fact]
    public async Task List_ReturnsSortedByName()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateLocationAsync(folder, "Città", "");
        await service.CreateLocationAsync(folder, "Foresta", "");

        var locations = await service.ListLocationsAsync(folder);

        Assert.Equal(new[] { "Città", "Foresta" }, locations.Select(l => l.Name).ToArray());
    }

    [Fact]
    public async Task List_WithSearch_FiltersByName()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await service.CreateLocationAsync(folder, "Taverna", "");
        await service.CreateLocationAsync(folder, "Castello", "");

        var matches = await service.ListLocationsAsync(folder, "cast");

        Assert.Equal("Castello", Assert.Single(matches).Name);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateLocationAsync(folder, "Vecchio Nome", "");

        var updated = await service.UpdateLocationAsync(folder, created.Id, "Nuovo Nome", "Descrizione");

        Assert.Equal("Nuovo Nome", updated.Name);
        Assert.Equal("Descrizione", updated.Description);
    }

    [Fact]
    public async Task Update_MissingLocation_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<LocationException>(
            () => service.UpdateLocationAsync(folder, Guid.NewGuid(), "X", ""));
    }

    [Fact]
    public async Task Delete_RemovesLocation()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateLocationAsync(folder, "Piazza", "");

        await service.DeleteLocationAsync(folder, created.Id);

        Assert.Empty(await service.ListLocationsAsync(folder));
    }

    [Fact]
    public async Task Delete_MissingLocation_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<LocationException>(() => service.DeleteLocationAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_EmptyName_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<LocationException>(() => service.CreateLocationAsync(folder, "  ", ""));
    }

    [Fact]
    public async Task Locations_PersistAcrossContexts()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateLocationAsync(folder, "Sotterranei", "Buio e pericolosi");

        await using var context = _factory.CreateContext(folder);
        var stored = await context.Locations.SingleAsync(l => l.Id == created.Id);

        Assert.Equal("Sotterranei", stored.Name);
        Assert.Equal("Buio e pericolosi", stored.Description);
    }

    [Fact]
    public async Task Create_WithNotes_PersistsNotes()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        var created = await service.CreateLocationAsync(folder, "Piazza", "", "Nota del luogo");

        Assert.Equal("Nota del luogo", created.Notes);
    }

    [Fact]
    public async Task Update_WithNotes_ChangesNotes()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateLocationAsync(folder, "Piazza", "", "");

        var updated = await service.UpdateLocationAsync(folder, created.Id, "Piazza", "", "Nuova nota");

        Assert.Equal("Nuova nota", updated.Notes);
    }

    [Fact]
    public async Task Get_ReturnsLocation()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        var created = await service.CreateLocationAsync(folder, "Foresta", "Verde");

        var loaded = await service.GetLocationAsync(folder, created.Id);

        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal("Foresta", loaded.Name);
    }

    [Fact]
    public async Task Get_MissingLocation_Throws()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();

        await Assert.ThrowsAsync<LocationException>(
            () => service.GetLocationAsync(folder, Guid.NewGuid()));
    }

    [Fact]
    public async Task Search_FiltersByNotes()
    {
        var folder = await CreateCampaignFolderAsync();
        var service = CreateService();
        await service.CreateLocationAsync(folder, "Taverna", "", "Frequenza: notturna");
        await service.CreateLocationAsync(folder, "Castello", "", "");

        var matches = await service.ListLocationsAsync(folder, "notturna");

        Assert.Equal("Taverna", Assert.Single(matches).Name);
    }
}