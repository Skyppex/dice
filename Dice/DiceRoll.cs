namespace Dice;

public readonly record struct DiceRoll(DiceRange DiceRange, params IRollModifier[] RollModifiers)
{
    public DiceResult Roll(IDiceRollHandlers handler)
    {
        List<float> rolls = new();
        List<DiceResult> diceResults = new();

        float total = handler.Handle(DiceRange);
        rolls.Add(total);

        foreach (IRollModifier t in RollModifiers)
        {
            DiceResult diceResult = t.Modify(total, DiceRange, rolls, handler);
            diceResults.Add(diceResult);
            total = diceResult.Value;
        }

        string expression = diceResults.Any()
            ? $"{string.Join(", ", diceResults.Select(dr => dr.Expression))}"
            : $"{total.ToString()}";
        
        return new DiceResult(total, expression);
    }
}

public interface IRollModifier
{
    public DiceResult Modify(float total, DiceRange diceRange, List<float> rolls, IDiceRollHandlers handler);
}

public record ExplodeModifier(int MaxExplosions = 1) : IRollModifier
{
    public DiceResult Modify(float total, DiceRange diceRange, List<float> rolls, IDiceRollHandlers handler)
    {
        float newTotal = ModifyRecurse(total, diceRange, total, rolls, handler);

        return rolls.Count <= 1
            ? new DiceResult(newTotal, $"{rolls.Single()}")
            : new DiceResult(newTotal, $"({string.Join(", ", rolls.Select(r => $"{r}!").Take(rolls.Count - 1))}, {rolls.Last()})");
    }

    private float ModifyRecurse(
        float total,
        DiceRange diceRange,
        float previousRoll,
        List<float> rolls,
        IDiceRollHandlers handler)
    {
        if (MaxExplosions is 0)
            return total;
        
        if (previousRoll == diceRange.Max)
            return HandleRoll(1f);

        if (handler.ExhaustiveRoll)
            return HandleRoll(MathF.Pow(1f / diceRange.Sides, rolls.Count));

        return total;

        float HandleRoll(float multiplier)
        {
            float explosion = multiplier * handler.Handle(diceRange);
            total += explosion;
            rolls.Add(explosion);

            return new ExplodeModifier(MaxExplosions - 1)
                .ModifyRecurse(total, diceRange, explosion, rolls, handler);
        }
    }
}


public record ReRollModifier(int MaxReRolls = 1) : IRollModifier
{
    public DiceResult Modify(float total, DiceRange diceRange, List<float> rolls, IDiceRollHandlers handler)
    {
        float newTotal = ModifyRecurse(total, diceRange, total, rolls, handler);
        return new DiceResult(newTotal, $"{rolls.Last()}{(rolls.Count > 1 ? 'r' : "")}");
    }

    private float ModifyRecurse(
        float total,
        DiceRange diceRange,
        float previousRoll,
        List<float> rolls,
        IDiceRollHandlers handler)
    {
        if (MaxReRolls is 0)
            return total;
        
        if (previousRoll == diceRange.Min)
            return HandleRoll(x => x);

        if (handler.ExhaustiveRoll)
        {
            float fraction = 1f / diceRange.Sides;
            return HandleRoll(x => x + x * MathF.Pow(fraction, rolls.Count) - MathF.Pow(fraction, rolls.Count));
        }

        return total;

        float HandleRoll(Func<float, float> alteration)
        {
            float reRoll = handler.Handle(diceRange);
            total = reRoll;
            rolls.Add(reRoll);

            return new ReRollModifier(MaxReRolls - 1)
                .ModifyRecurse(total, diceRange, reRoll, rolls, handler);
        }
    }
}