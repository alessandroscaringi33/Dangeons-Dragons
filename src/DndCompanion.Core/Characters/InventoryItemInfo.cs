namespace DndCompanion.Core.Characters;

/// <summary>Presentation of an inventory item carried by a character.</summary>
public sealed class InventoryItemInfo
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Quantity { get; init; } = 1;

    public double Weight { get; init; }

    public string Notes { get; init; } = string.Empty;
}