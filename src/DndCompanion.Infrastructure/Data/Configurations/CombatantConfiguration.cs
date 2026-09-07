using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class CombatantConfiguration : IEntityTypeConfiguration<Combatant>
{
    public void Configure(EntityTypeBuilder<Combatant> builder)
    {
        builder.ToTable("Combatants");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.CombatId);

        builder.HasOne<Combat>()
            .WithMany(c => c.Combatants)
            .HasForeignKey(c => c.CombatId)
            .OnDelete(DeleteBehavior.Cascade);

        // A combatant may reference either a character or an NPC. Both links
        // are optional because a combat keeps a snapshot of the combatant's
        // own data (name, HP, AC). If the source entity is deleted, the link
        // is cleared rather than the combat history being removed.
        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(c => c.CharacterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Npc>()
            .WithMany()
            .HasForeignKey(c => c.NpcId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
