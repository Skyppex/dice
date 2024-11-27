namespace Dice;

public interface IDiceRollHandlers
{
    public bool ExhaustiveRoll { get; }
    public float Handle(IDice dice);
}

public record RandomRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;

    public float Handle(IDice dice) => dice.Roll();
}

public record MaxRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;

    public float Handle(IDice dice) => dice.Max;
}

public record MinRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;

    public float Handle(IDice dice) => dice.Min;
}

public record MedianRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => false;

    public float Handle(IDice dice) => (dice.Min + dice.Max) / 2f;
}

public record AverageRollHandler : IDiceRollHandlers
{
    public bool ExhaustiveRoll => true;

    public float Handle(IDice dice) =>
        dice.SideValues.Select(v => (float)v).Average();
}
