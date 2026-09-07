using DndCompanion.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DndCompanion.Infrastructure.Data.Configurations;

public sealed class NpcConfiguration : IEntityTypeConfiguration<Npc>
{
    public void Configure(EntityTypeBuilder<Npc> builder)
    {
        builder.ToTable("Npcs");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Role)
            .HasMaxLength(200);

        builder.Property(n => n.Description)
            .HasMaxLength(4000);

        builder.Property(n => n.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(n => n.CampaignId);
    }
}
