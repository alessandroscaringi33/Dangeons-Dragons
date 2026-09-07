using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.ToTable("Chapters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.Description)
            .HasMaxLength(4000);

        builder.Property(c => c.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(c => c.CampaignId);

        builder.HasIndex(c => new { c.CampaignId, c.Order })
            .IsUnique();

        builder.HasMany(c => c.Scenes)
            .WithOne()
            .HasForeignKey(s => s.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
