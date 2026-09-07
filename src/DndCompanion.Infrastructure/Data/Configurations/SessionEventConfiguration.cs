using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class SessionEventConfiguration : IEntityTypeConfiguration<SessionEvent>
{
    public void Configure(EntityTypeBuilder<SessionEvent> builder)
    {
        builder.ToTable("SessionEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(e => e.Timestamp)
            .HasColumnType("TEXT");

        builder.HasIndex(e => new { e.SessionId, e.Timestamp });

        // The "related" ids are loose references used to link events to domain
        // objects for display/filtering. They are persisted as plain nullable
        // columns without foreign keys, so history is never lost when a
        // related entity is removed.
        builder.Property(e => e.RelatedCharacterId);
        builder.Property(e => e.RelatedNpcId);
        builder.Property(e => e.RelatedSceneId);
        builder.Property(e => e.RelatedDiceRollId);
        builder.Property(e => e.RelatedCombatId);
    }
}
