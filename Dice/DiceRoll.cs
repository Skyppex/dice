namespace Dice;

public readonly record struct DiceRoll(DiceRange DiceRange, params IRollModifier[] RollModifiers)
{
    public DiceResultInt Roll(IDiceRollHandlers handler)
    {
        List<int> rolls = new();
        List<DiceResultInt> diceResults = new();

        int total = handler.Handle(DiceRange);
        rolls.Add(total);

        foreach (IRollModifier t in RollModifiers)
        {
            DiceResultInt diceResult = t.Modify(total, DiceRange, rolls, handler);
            diceResults.Add(diceResult);
            total = diceResult.Value;
        }

        string expression = diceResults.Any()
            ? $"{string.Join(", ", diceResults.Select(dr => dr.Expression))}"
            : $"{total.ToString()}";
        
        return new DiceResultInt(total, expression);
    }
}

public interface IRollModifier
{
    public DiceResultInt Modify(int total, DiceRange diceRange, List<int> rolls, IDiceRollHandlers handler);
}

public record ExplodeModifier(int MaxExplosions = 1) : IRollModifier
{
    public DiceResultInt Modify(int total, DiceRange diceRange, List<int> rolls, IDiceRollHandlers handler)
    {
        int newTotal = ModifyRecurse(total, diceRange, total, rolls, handler);

        return rolls.Count <= 1
            ? new DiceResultInt(newTotal, $"{rolls.Single()}")
            : new DiceResultInt(newTotal, $"({string.Join(", ", rolls.Select(r => $"{r}!").Take(rolls.Count - 1))}, {rolls.Last()})");
    }

    private int ModifyRecurse(int total, DiceRange diceRange, int previousRoll, List<int> rolls, IDiceRollHandlers handler)
    {
        if (MaxExplosions is 0)
            return total;
        
        if (previousRoll == diceRange.Max)
        {
            int explosion = handler.Handle(diceRange);
            total += explosion;
            rolls.Add(explosion);
            return new ExplodeModifier(MaxExplosions - 1).ModifyRecurse(total, diceRange, explosion, rolls, handler);
        }

        return total;
    }
}