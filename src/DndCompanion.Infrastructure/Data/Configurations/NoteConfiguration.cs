using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .HasMaxLength(300);

        builder.Property(n => n.Content)
            .HasMaxLength(8000);

        builder.Property(n => n.CreatedAt)
            .HasColumnType("TEXT");

        builder.Property(n => n.UpdatedAt)
            .HasColumnType("TEXT");

        builder.HasIndex(n => new { n.CampaignId, n.IsPinned });

        builder.HasIndex(n => n.SessionId);
    }
}
