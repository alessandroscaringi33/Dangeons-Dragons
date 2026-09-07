using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class DiceRollConfiguration : IEntityTypeConfiguration<DiceRoll>
{
    public void Configure(EntityTypeBuilder<DiceRoll> builder)
    {
        builder.ToTable("DiceRolls");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DiceNotation)
            .HasMaxLength(50);

        builder.Property(d => d.Purpose)
            .HasMaxLength(400);

        builder.Property(d => d.Timestamp)
            .HasColumnType("TEXT");

        builder.Property(d => d.Results)
            .HasColumnType("TEXT")
            .HasConversion(
                v => string.Join(',', v),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<int>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList());

        builder.HasIndex(d => new { d.SessionId, d.Timestamp });

        // Loose reference to the acting character; no FK so dice history is
        // preserved even if the character is later removed.
        builder.Property(d => d.CharacterId);
    }
}
