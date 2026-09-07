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

        builder.HasIndex(q => q.ChapterId);

        builder.HasIndex(q => q.SceneId);

        builder.HasIndex(q => q.LocationId);

        builder.HasIndex(q => q.NpcId);

        builder.HasOne(q => q.Chapter)
            .WithMany()
            .HasForeignKey(q => q.ChapterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(q => q.Scene)
            .WithMany()
            .HasForeignKey(q => q.SceneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(q => q.Location)
            .WithMany()
            .HasForeignKey(q => q.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(q => q.Npc)
            .WithMany()
            .HasForeignKey(q => q.NpcId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
