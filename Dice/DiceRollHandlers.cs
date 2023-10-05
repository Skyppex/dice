namespace Dice;

public interface IDiceRollHandlers
{
    public bool ExhaustiveRoll { get; }
    public float Handle(DiceRange diceRange);
}

public record RandomRollHandler(Random Random) : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;
    public float Handle(DiceRange diceRange) => Random.Next(diceRange.Min, diceRange.Max + 1);
}

public record MaxRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;
    public float Handle(DiceRange diceRange) => diceRange.Max;
}

public record MinRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;
    public float Handle(DiceRange diceRange) => diceRange.Min;
}

public record MedianRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;
    public float Handle(DiceRange diceRange) => (diceRange.Min + diceRange.Max) / 2f;
}

public record AverageRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => true;
    public float Handle(DiceRange diceRange) => (diceRange.Min + diceRange.Max) / 2f;
}
