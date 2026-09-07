using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class QuestConfiguration : IEntityTypeConfiguration<Quest>
{
    public void Configure(EntityTypeBuilder<Quest> builder)
    {
        builder.ToTable("Quests");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(q => q.Description)
            .HasMaxLength(4000);

        builder.Property(q => q.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(q => q.CampaignId);

        builder.HasIndex(q => new { q.CampaignId, q.Status });
    }
}
