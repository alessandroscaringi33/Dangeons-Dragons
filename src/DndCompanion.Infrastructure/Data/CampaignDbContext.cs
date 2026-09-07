using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Data;

/// <summary>
/// Entity Framework Core context for a single campaign. Every campaign has its
/// own SQLite database file, so this context is never shared across campaigns.
/// </summary>
public sealed class CampaignDbContext : DbContext
{
    public CampaignDbContext(DbContextOptions<CampaignDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<Chapter> Chapters => Set<Chapter>();

    public DbSet<Scene> Scenes => Set<Scene>();

    public DbSet<Character> Characters => Set<Character>();

    public DbSet<Npc> Npcs => Set<Npc>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Quest> Quests => Set<Quest>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<SessionEvent> SessionEvents => Set<SessionEvent>();

    public DbSet<DiceRoll> DiceRolls => Set<DiceRoll>();

    public DbSet<Combat> Combats => Set<Combat>();

    public DbSet<Combatant> Combatants => Set<Combatant>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampaignDbContext).Assembly);
    }
}