namespace Dice;

public interface IDiceRollHandlers
{
    public int Handle(DiceRange diceRange);
}

public record RandomRollHandler(Random Random) : IDiceRollHandlers
{
    public int Handle(DiceRange diceRange) => Random.Next(diceRange.Min, diceRange.Max + 1);
}

public record MaxRollHandler : IDiceRollHandlers
{
    public int Handle(DiceRange diceRange) => diceRange.Max;
}

public record MinRollHandler : IDiceRollHandlers
{
    public int Handle(DiceRange diceRange) => diceRange.Min;
}

public record MedianRollHandler : IDiceRollHandlers
{
    public int Handle(DiceRange diceRange) => (diceRange.Min + diceRange.Max) / 2;
}
