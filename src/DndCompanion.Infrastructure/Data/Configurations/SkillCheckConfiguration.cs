using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class SkillCheckConfiguration : IEntityTypeConfiguration<SkillCheck>
{
    public void Configure(EntityTypeBuilder<SkillCheck> builder)
    {
        builder.ToTable("SkillChecks");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Skill)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Timestamp)
            .HasColumnType("TEXT");

        builder.HasIndex(s => s.SessionId);

        builder.HasIndex(s => new { s.SessionId, s.Timestamp });

        // The character link is a loose reference kept even if the character
        // is later removed, so the check history is never lost.
        builder.Property(s => s.CharacterId);
    }
}