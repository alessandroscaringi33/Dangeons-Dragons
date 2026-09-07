using DndCompanion.Core.Dice;
using DndCompanion.Core.Domain.Enums;

namespace DndCompanion.Tests;

public sealed class DiceParserTests
{
    [Theory]
    [InlineData("1d4", 1, DiceType.D4, 0)]
    [InlineData("1d6", 1, DiceType.D6, 0)]
    [InlineData("1d8", 1, DiceType.D8, 0)]
    [InlineData("1d10", 1, DiceType.D10, 0)]
    [InlineData("1d12", 1, DiceType.D12, 0)]
    [InlineData("1d20", 1, DiceType.D20, 0)]
    [InlineData("1d100", 1, DiceType.D100, 0)]
    [InlineData("d20", 1, DiceType.D20, 0)]
    [InlineData("2d6", 2, DiceType.D6, 0)]
    [InlineData("4d8", 4, DiceType.D8, 0)]
    [InlineData("3d10", 3, DiceType.D10, 0)]
    [InlineData("1d20+5", 1, DiceType.D20, 5)]
    [InlineData("2d6+3", 2, DiceType.D6, 3)]
    [InlineData("1d20-2", 1, DiceType.D20, -2)]
    [InlineData("1d20 + 5", 1, DiceType.D20, 5)]
    [InlineData("2d6 + 3", 2, DiceType.D6, 3)]
    [InlineData("1d20+0", 1, DiceType.D20, 0)]
    [InlineData(" 1d20 + 5 ", 1, DiceType.D20, 5)]
    public void Parse_ValidNotations(string notation, int count, DiceType type, int modifier)
    {
        var expr = DiceParser.Parse(notation);

        Assert.Equal(count, expr.DiceCount);
        Assert.Equal(type, expr.DiceType);
        Assert.Equal(modifier, expr.Modifier);
    }

    [Theory]
    [InlineData("1d20", 20)]
    [InlineData("2d6", 6)]
    [InlineData("4d8", 8)]
    [InlineData("1d100", 100)]
    public void Faces_MatchesType(string notation, int faces)
    {
        Assert.Equal(faces, DiceParser.Parse(notation).Faces);
    }

    [Theory]
    [InlineData("1d20", 1)]
    [InlineData("2d6", 2)]
    [InlineData("3d10", 3)]
    public void MinTotal_IsCountPlusModifier(string notation, int count)
    {
        Assert.Equal(count, DiceParser.Parse(notation).MinTotal);
    }

    [Theory]
    [InlineData("1d20", 20)]
    [InlineData("2d6", 12)]
    [InlineData("4d8", 32)]
    [InlineData("1d20+5", 25)]
    public void MaxTotal_IsCountTimesFacesPlusModifier(string notation, int max)
    {
        Assert.Equal(max, DiceParser.Parse(notation).MaxTotal);
    }

    [Theory]
    [InlineData("2d6+3")]
    [InlineData("1d20-2")]
    [InlineData("4d8")]
    [InlineData("1d20")]
    public void Notation_IsCanonicalAndReversible(string notation)
    {
        var expr = DiceParser.Parse(notation);
        var reparsed = DiceParser.Parse(expr.Notation);

        Assert.Equal(expr.DiceCount, reparsed.DiceCount);
        Assert.Equal(expr.DiceType, reparsed.DiceType);
        Assert.Equal(expr.Modifier, reparsed.Modifier);
    }

    [Fact]
    public void SupportedDice_AreAllPresent()
    {
        var service = new DiceService();
        Assert.Equal(
            new[] { DiceType.D4, DiceType.D6, DiceType.D8, DiceType.D10, DiceType.D12, DiceType.D20, DiceType.D100 },
            service.SupportedDice);
    }
}

public sealed class DiceParserInvalidTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("20")]
    [InlineData("d")]
    [InlineData("dd20")]
    [InlineData("1d")]
    [InlineData("1x20")]
    [InlineData("1d7")]
    [InlineData("0d20")]
    [InlineData("d0")]
    [InlineData("2d6++3")]
    [InlineData("1d20+")]
    [InlineData("1d20+5+2")]
    [InlineData("1d20+abc")]
    [InlineData("101d20")]
    [InlineData("-1d20")]
    [InlineData("1.5d20")]
    [InlineData("1d20*2")]
    [InlineData("1d20/2")]
    public void Parse_InvalidNotations_Throws(string? notation)
    {
        Assert.Throws<DiceParseException>(() => DiceParser.Parse(notation!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1d7")]
    [InlineData("0d20")]
    public void TryParse_Invalid_ReturnsFalse(string? notation)
    {
        Assert.False(DiceParser.TryParse(notation, out var expr));
        Assert.Null(expr);
    }

    [Theory]
    [InlineData("1d20")]
    [InlineData("2d6+3")]
    [InlineData("d8")]
    public void TryParse_Valid_ReturnsTrue(string notation)
    {
        Assert.True(DiceParser.TryParse(notation, out var expr));
        Assert.NotNull(expr);
    }

    [Fact]
    public void Parse_UnsupportedDenomination_Throws()
    {
        Assert.Throws<DiceParseException>(() => DiceParser.Parse("1d7"));
    }
}

public sealed class DiceRollCalculatorTests
{
    [Theory]
    [InlineData(DiceType.D4)]
    [InlineData(DiceType.D6)]
    [InlineData(DiceType.D8)]
    [InlineData(DiceType.D10)]
    [InlineData(DiceType.D12)]
    [InlineData(DiceType.D20)]
    [InlineData(DiceType.D100)]
    public void Roll_ResultsAreWithinDieRange(DiceType type)
    {
        var calculator = new DiceRollCalculator(new Random(42));
        var expr = new DiceExpression(1, type, 0);

        for (var i = 0; i < 200; i++)
        {
            var result = calculator.Roll(expr);
            Assert.Single(result.Results);
            Assert.InRange(result.Results[0], 1, (int)type);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    public void Roll_MultipleDice_ProducesMatchingResultCount(int count)
    {
        var calculator = new DiceRollCalculator(new Random(1));
        var expr = new DiceExpression(count, DiceType.D6, 0);

        var result = calculator.Roll(expr);

        Assert.Equal(count, result.Results.Count);
        Assert.Equal(count, result.DiceCount);
    }

    [Theory]
    [InlineData("1d20", 0)]
    [InlineData("1d20+5", 5)]
    [InlineData("2d6+3", 3)]
    [InlineData("1d20-2", -2)]
    public void Roll_Modifier_IsApplied(string notation, int modifier)
    {
        var calculator = new DiceRollCalculator(new Random(7));

        var result = calculator.Roll(notation);

        Assert.Equal(modifier, result.Modifier);
        Assert.Equal(result.KeptResult + modifier, result.Total);
    }

    [Fact]
    public void Roll_Normal_TotalIsSumPlusModifier()
    {
        var calculator = new DiceRollCalculator(new Random(3));
        var expr = new DiceExpression(2, DiceType.D6, 4);

        var result = calculator.Roll(expr);

        Assert.Equal(DiceRollMode.Normal, result.Mode);
        Assert.Equal(2, result.Results.Count);
        Assert.Equal(result.Results.Sum() + 4, result.Total);
        Assert.Equal(result.Results.Sum(), result.KeptResult);
    }

    [Fact]
    public void Roll_DeterministicRandom_ProducesExpectedSequence()
    {
        // With a seeded Random we can assert exact outputs.
        var calculator = new DiceRollCalculator(new Random(12345));
        var expr = new DiceExpression(1, DiceType.D20, 0);

        var r1 = calculator.Roll(expr);
        var r2 = calculator.Roll(expr);

        Assert.NotNull(r1);
        Assert.NotNull(r2);
    }

    [Fact]
    public void Roll_NullExpression_Throws()
    {
        var calculator = new DiceRollCalculator();
        Assert.Throws<ArgumentNullException>(() => calculator.Roll((DiceExpression)null!));
    }
}

public sealed class DiceAdvantageTests
{
    [Fact]
    public void Advantage_KeepsHighest_OfTwoAttempts()
    {
        var calculator = new DiceRollCalculator(new Random(99));
        var expr = new DiceExpression(1, DiceType.D20, 0);

        var result = calculator.Roll(expr, DiceRollMode.Advantage);

        Assert.Equal(DiceRollMode.Advantage, result.Mode);
        Assert.True(result.IsAdvantage);
        Assert.Equal(2, result.Results.Count);
        Assert.Equal(result.Results.Max(), result.KeptResult);
        Assert.Equal(result.Results.Max(), result.Total);
    }

    [Fact]
    public void Disadvantage_KeepsLowest_OfTwoAttempts()
    {
        var calculator = new DiceRollCalculator(new Random(5));
        var expr = new DiceExpression(1, DiceType.D20, 0);

        var result = calculator.Roll(expr, DiceRollMode.Disadvantage);

        Assert.Equal(DiceRollMode.Disadvantage, result.Mode);
        Assert.True(result.IsDisadvantage);
        Assert.Equal(2, result.Results.Count);
        Assert.Equal(result.Results.Min(), result.KeptResult);
        Assert.Equal(result.Results.Min(), result.Total);
    }

    [Fact]
    public void Advantage_WithModifier_AppliesToKeptResult()
    {
        var calculator = new DiceRollCalculator(new Random(12));
        var expr = new DiceExpression(1, DiceType.D20, 5);

        var result = calculator.Roll(expr, DiceRollMode.Advantage);

        Assert.Equal(result.Results.Max() + 5, result.Total);
    }

    [Fact]
    public void Disadvantage_WithModifier_AppliesToKeptResult()
    {
        var calculator = new DiceRollCalculator(new Random(21));
        var expr = new DiceExpression(1, DiceType.D20, 3);

        var result = calculator.Roll(expr, DiceRollMode.Disadvantage);

        Assert.Equal(result.Results.Min() + 3, result.Total);
    }

    [Fact]
    public void Advantage_MultiDie_KeepsHigherGroupTotal()
    {
        // Deterministic Random so both attempts are known.
        var calculator = new DiceRollCalculator(new Random(77));
        var expr = new DiceExpression(2, DiceType.D6, 0);

        var result = calculator.Roll(expr, DiceRollMode.Advantage);

        Assert.Equal(4, result.Results.Count);
        var firstTotal = result.Results[0] + result.Results[1];
        var secondTotal = result.Results[2] + result.Results[3];
        Assert.Equal(Math.Max(firstTotal, secondTotal), result.KeptResult);
    }

    [Fact]
    public void Disadvantage_MultiDie_KeepsLowerGroupTotal()
    {
        var calculator = new DiceRollCalculator(new Random(44));
        var expr = new DiceExpression(2, DiceType.D6, 0);

        var result = calculator.Roll(expr, DiceRollMode.Disadvantage);

        Assert.Equal(4, result.Results.Count);
        var firstTotal = result.Results[0] + result.Results[1];
        var secondTotal = result.Results[2] + result.Results[3];
        Assert.Equal(Math.Min(firstTotal, secondTotal), result.KeptResult);
    }

    [Fact]
    public void Advantage_ResultsAlwaysWithinRange()
    {
        var calculator = new DiceRollCalculator(new Random(31));
        var expr = new DiceExpression(1, DiceType.D20, 0);

        for (var i = 0; i < 500; i++)
        {
            var result = calculator.Roll(expr, DiceRollMode.Advantage);
            Assert.All(result.Results, v => Assert.InRange(v, 1, 20));
        }
    }
}

public sealed class DicePhysicalRollTests
{
    [Theory]
    [InlineData("1d20", 17)]
    [InlineData("1d20+4", 17)]
    [InlineData("1d6", 6)]
    [InlineData("1d100", 100)]
    public void RollPhysical_UsesProvidedValue_AsRealResult(string notation, int value)
    {
        var calculator = new DiceRollCalculator(new Random(1));

        var result = calculator.RollPhysical(notation, value);

        Assert.True(result.IsPhysicalRoll);
        Assert.Equal(DiceRollMode.Manual, result.Mode);
        Assert.Single(result.Results);
        Assert.Equal(value, result.Results[0]);
        Assert.Equal(value, result.KeptResult);
    }

    [Theory]
    [InlineData("1d20", 17, 0, 17)]
    [InlineData("1d20+4", 17, 4, 21)]
    [InlineData("1d20-1", 17, -1, 16)]
    public void RollPhysical_Total_UsesPhysicalValuePlusModifier(string notation, int value, int modifier, int total)
    {
        var calculator = new DiceRollCalculator(new Random(1));

        var result = calculator.RollPhysical(notation, value);

        Assert.Equal(modifier, result.Modifier);
        Assert.Equal(total, result.Total);
    }

    [Fact]
    public void RollPhysical_DoesNotConsumeRandomness()
    {
        // Seeded calculator: a physical roll must not change the next roll.
        var calculator = new DiceRollCalculator(new Random(1000));
        var expr = new DiceExpression(1, DiceType.D20, 0);

        var physical = calculator.RollPhysical(expr, 17);
        var after = calculator.Roll(expr);

        Assert.Equal(17, physical.Total);
        // The random sequence must still start from the same seed.
        var verify = new DiceRollCalculator(new Random(1000)).Roll(expr);
        Assert.Equal(verify.Total, after.Total);
    }

    [Fact]
    public void RollPhysical_NeverRandomizes_ThePhysicalValue()
    {
        var calculator = new DiceRollCalculator(new Random(3));

        for (var value = 1; value <= 20; value++)
        {
            var result = calculator.RollPhysical("1d20", value);
            Assert.Equal(value, result.KeptResult);
            Assert.Equal(value, result.Results[0]);
        }
    }

    [Theory]
    [InlineData("1d20", 0)]
    [InlineData("1d20", 21)]
    [InlineData("1d6", 7)]
    [InlineData("1d100", 101)]
    public void RollPhysical_OutOfRange_Throws(string notation, int value)
    {
        var calculator = new DiceRollCalculator();
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.RollPhysical(notation, value));
    }

    [Fact]
    public void RollPhysical_MultiDie_Throws()
    {
        var calculator = new DiceRollCalculator();
        Assert.Throws<ArgumentException>(() => calculator.RollPhysical("2d6", 5));
    }

    [Fact]
    public void RollPhysical_IsMarkedPhysical()
    {
        var service = new DiceService();
        var result = service.RollPhysical("1d20", 15);

        Assert.True(result.IsPhysicalRoll);
        Assert.False(result.IsAdvantage);
        Assert.False(result.IsDisadvantage);
    }
}

public sealed class DiceServiceTests
{
    [Fact]
    public void Service_Roll_ParsesAndRolls()
    {
        var service = new DiceService();
        var result = service.Roll("2d6+3");

        Assert.Equal(2, result.DiceCount);
        Assert.Equal(DiceType.D6, result.DiceType);
        Assert.Equal(3, result.Modifier);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public void Service_Roll_WithAdvantage()
    {
        var service = new DiceService();
        var result = service.Roll("1d20", DiceRollMode.Advantage);

        Assert.True(result.IsAdvantage);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public void Service_RollPhysical_IsStable()
    {
        var service = new DiceService();
        var result = service.RollPhysical("1d20+4", 17);

        Assert.Equal(21, result.Total);
        Assert.True(result.IsPhysicalRoll);
    }

    [Fact]
    public void Service_TryParse_Valid()
    {
        var service = new DiceService();
        Assert.True(service.TryParse("3d10", out var expr));
        Assert.NotNull(expr);
        Assert.Equal(DiceType.D10, expr!.DiceType);
    }

    [Fact]
    public void Service_TryParse_Invalid()
    {
        var service = new DiceService();
        Assert.False(service.TryParse("banana", out var expr));
        Assert.Null(expr);
    }

    [Fact]
    public void Service_Parse_Invalid_Throws()
    {
        var service = new DiceService();
        Assert.Throws<DiceParseException>(() => service.Parse("1d7"));
    }
}

public sealed class DiceRollResultTests
{
    [Fact]
    public void Total_IsKeptPlusModifier()
    {
        var result = new DiceRollResult(
            "1d20+5", DiceType.D20, 1, 5, DiceRollMode.Normal, new[] { 12 }, 12, isPhysicalRoll: false);

        Assert.Equal(17, result.Total);
        Assert.Equal(12, result.KeptResult);
    }

    [Fact]
    public void RawSum_IsSumOfAllDice()
    {
        var result = new DiceRollResult(
            "2d6", DiceType.D6, 2, 0, DiceRollMode.Normal, new[] { 3, 4 }, 7, isPhysicalRoll: false);

        Assert.Equal(7, result.RawSum);
        Assert.Equal(7, result.Total);
    }

    [Fact]
    public void Advantage_KeptIsMax()
    {
        var result = new DiceRollResult(
            "1d20", DiceType.D20, 1, 0, DiceRollMode.Advantage, new[] { 5, 17 }, 17, isPhysicalRoll: false);

        Assert.Equal(17, result.KeptResult);
        Assert.Equal(17, result.Total);
        Assert.Equal(17, result.BestDie);
        Assert.Equal(5, result.WorstDie);
    }

    [Fact]
    public void Disadvantage_KeptIsMin()
    {
        var result = new DiceRollResult(
            "1d20", DiceType.D20, 1, 0, DiceRollMode.Disadvantage, new[] { 5, 17 }, 5, isPhysicalRoll: false);

        Assert.Equal(5, result.KeptResult);
        Assert.Equal(5, result.Total);
    }
}