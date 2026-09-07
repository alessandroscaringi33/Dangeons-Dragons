using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(s => s.Summary)
            .HasMaxLength(4000);

        builder.Property(s => s.Notes)
            .HasMaxLength(4000);

        builder.Property(s => s.StartedAt)
            .HasColumnType("TEXT");

        builder.Property(s => s.EndedAt)
            .HasColumnType("TEXT");

        builder.HasIndex(s => s.CampaignId);

        builder.HasIndex(s => new { s.CampaignId, s.Number })
            .IsUnique();

        builder.HasIndex(s => new { s.CampaignId, s.IsActive });

        builder.HasMany(s => s.Events)
            .WithOne()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.DiceRolls)
            .WithOne()
            .HasForeignKey(d => d.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Combats)
            .WithOne()
            .HasForeignKey(c => c.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.SessionNotes)
            .WithOne()
            .HasForeignKey(n => n.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.SkillChecks)
            .WithOne()
            .HasForeignKey(sk => sk.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
