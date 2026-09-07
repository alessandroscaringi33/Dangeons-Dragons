using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("Characters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.PlayerName)
            .HasMaxLength(200);

        builder.Property(c => c.Class)
            .HasMaxLength(100);

        builder.Property(c => c.Subclass)
            .HasMaxLength(100);

        builder.Property(c => c.Race)
            .HasMaxLength(100);

        builder.Property(c => c.Background)
            .HasMaxLength(100);

        builder.Property(c => c.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(c => c.CampaignId);

        builder.HasMany(c => c.Inventory)
            .WithOne()
            .HasForeignKey(i => i.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
