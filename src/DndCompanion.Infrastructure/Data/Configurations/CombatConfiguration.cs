using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class CombatConfiguration : IEntityTypeConfiguration<Combat>
{
    public void Configure(EntityTypeBuilder<Combat> builder)
    {
        builder.ToTable("Combats");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.StartedAt)
            .HasColumnType("TEXT");

        builder.Property(c => c.EndedAt)
            .HasColumnType("TEXT");

        builder.HasIndex(c => new { c.SessionId, c.IsActive });

        // Computed, read-only helpers that must not be treated as navigations
        // by EF Core conventions.
        builder.Ignore(c => c.ActiveCombatantsInOrder);
        builder.Ignore(c => c.CurrentCombatant);

        // The Combatant relationship is defined in CombatantConfiguration to
        // keep all FKs of the dependent entity in a single place.
    }
}
