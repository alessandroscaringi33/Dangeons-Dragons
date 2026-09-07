namespace DndCompanion.Core.Domain.Entities;

/// <summary>An item carried by a character.</summary>
public class InventoryItem : Entity
{
    public Guid CharacterId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public double Weight { get; set; }

    public string Notes { get; set; } = string.Empty;
}
