using System.Text.RegularExpressions;
using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Core.Dice;

/// <summary>
/// Parses dice notation strings such as <c>1d20</c>, <c>1d20+5</c>,
/// <c>2d6+3</c>, <c>4d8</c> into a <see cref="DiceExpression"/>. Fully
/// independent from the UI.
/// </summary>
public static partial class DiceParser
{
    // Matches optional count, a die denomination and an optional modifier.
    // Examples: "2d6+3", "1d20-2", "d8", "3d100".
    [GeneratedRegex(
        @"^\s*(?<count>\d{1,2})?\s*d\s*(?<faces>\d{1,3})\s*(?<mod>[+-]\s*\d{1,3})?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiceRegex();

    /// <summary>
    /// Parses a dice notation string. The count defaults to 1 when omitted.
    /// </summary>
    /// <exception cref="DiceParseException">When the notation is not valid.</exception>
    public static DiceExpression Parse(string notation)
    {
        if (string.IsNullOrWhiteSpace(notation))
        {
            throw new DiceParseException("La notazione dei dadi è vuota.");
        }

        var match = DiceRegex().Match(notation);
        if (!match.Success)
        {
            throw new DiceParseException($"Notazione non valida: '{notation}'.");
        }

        var countText = match.Groups["count"].Value;
        var facesText = match.Groups["faces"].Value;
        var modText = match.Groups["mod"].Value;

        var count = string.IsNullOrEmpty(countText) ? 1 : int.Parse(countText);
        var faces = int.Parse(facesText);
        var modifier = string.IsNullOrEmpty(modText) ? 0 : int.Parse(modText.Replace(" ", string.Empty));

        if (count <= 0)
        {
            throw new DiceParseException($"Il numero di dadi deve essere positivo ('{notation}').");
        }

        if (count > 100)
        {
            throw new DiceParseException($"Troppi dadi ('{notation}').");
        }

        var diceType = DiceTypeFromFaces(faces, notation);

        return new DiceExpression(count, diceType, modifier);
    }

    /// <summary>Attempts to parse; returns false instead of throwing on invalid input.</summary>
    public static bool TryParse(string? notation, out DiceExpression? expression)
    {
        try
        {
            expression = Parse(notation ?? string.Empty);
            return true;
        }
        catch (DiceParseException)
        {
            expression = null;
            return false;
        }
    }

    private static DiceType DiceTypeFromFaces(int faces, string notation)
    {
        foreach (var value in Enum.GetValues<DiceType>())
        {
            if ((int)value == faces)
            {
                return value;
            }
        }

        throw new DiceParseException($"Denominazione del dado non supportata ('{notation}'): d{faces}.");
    }
}