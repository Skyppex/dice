namespace Dice;

public readonly record struct DiceRoll(IDice Dice, params IRollModifier[] RollModifiers)
{
    public DiceResult Roll(IDiceRollHandlers handler)
    {
        List<DiceResult> diceResults = new();

        float total = handler.Handle(Dice);

        IDice diceToUse = Dice;
        
        foreach (IRollModifier t in RollModifiers)
        {
            DiceResult diceResult = t.Modify(total, diceToUse, handler, out diceToUse);
            diceResults.Add(diceResult);
            total = diceResult.Value;
        }

        diceResults.Reverse();
        string expression = diceResults.Count switch
        {
            > 1 => $"{string.Join(" <- ", diceResults.Select(dr => dr.Expression))}",
            1 => diceResults.Single().Expression,
            _ => diceToUse.Format(total),
        };
        
        return new DiceResult(total, expression);
    }
}

public interface IRollModifier
{
    public DiceResult Modify(float total, IDice dice, IDiceRollHandlers handler, out IDice newDice);
}

public record ExplodeModifier(int MaxExplosions = 1, bool Combined = false) : IRollModifier
{
    public DiceResult Modify(float total, IDice dice, IDiceRollHandlers handler, out IDice newDice)
    {
        newDice = dice;
        List<float> rolls = new() { total };
        float newTotal = ModifyRecurse(total, dice, total, rolls, handler);
        
        if (Combined)
            return new DiceResult(newTotal, $"{dice.Format(newTotal)}{(rolls.Count > 1 ? new string('!', rolls.Count - 1) : string.Empty)}");
            
        return rolls.Count <= 1
            ? new DiceResult(newTotal, dice.Format(newTotal))
            : new DiceResult(newTotal, $"({string.Join(", ", rolls.Select(r => $"{dice.Format(r)}!").Take(rolls.Count - 1))}, {rolls.Last()})");
    }

    private float ModifyRecurse(
        float total,
        IDice dice,
        float previousRoll,
        List<float> rolls,
        IDiceRollHandlers handler)
    {
        if (MaxExplosions is 0)
            return total;
        
        if (previousRoll == dice.Max)
            return HandleRoll(1f);

        if (handler.ExhaustiveRoll)
            return HandleRoll(MathF.Pow(1f / dice.Sides, rolls.Count));

        return total;

        float HandleRoll(float multiplier)
        {
            float explosion = multiplier * handler.Handle(dice);
            rolls.Add(explosion);
            total += explosion;

            return new ExplodeModifier(MaxExplosions - 1)
                .ModifyRecurse(total, dice, explosion, rolls, handler);
        }
    }
}

public record ReRollModifier(int MaxReRolls = 1) : IRollModifier
{
    public DiceResult Modify(float total, IDice dice, IDiceRollHandlers handler, out IDice newDice)
    {
        newDice = dice;
        List<float> rolls = new() { total };
        float newTotal = ModifyRecurse(total, dice, total, rolls, handler);
        return new DiceResult(newTotal, $"{dice.Format(rolls.Last())}{(rolls.Count > 1 ? 'r' : "")}");
    }

    private float ModifyRecurse(
        float total,
        IDice dice,
        float previousRoll,
        List<float> rolls,
        IDiceRollHandlers handler)
    {
        if (MaxReRolls is 0)
            return total;
        
        if (previousRoll == dice.Min)
            return HandleRoll();

        if (handler.ExhaustiveRoll)
            throw new NotSupportedException("Exhaustive roll is not supported for re-rolls.");

        return total;

        float HandleRoll()
        {
            float reRoll = handler.Handle(dice);
            total = reRoll;
            rolls.Add(reRoll);

            return new ReRollModifier(MaxReRolls - 1)
                .ModifyRecurse(total, dice, reRoll, rolls, handler);
        }
    }
}

public record ConditionModifier(List<ConditionModifier.Condition> Conditions) : IRollModifier
{
    public DiceResult Modify(float total, IDice dice, IDiceRollHandlers handler, out IDice newDice)
    {
        newDice = new DiceValues(new[] {0, 1}, IDice.DefaultFormat);

        if (handler.ExhaustiveRoll)
        {
            float chanceOfSuccess = Enumerable.Range(dice.Min, dice.Sides)
                .Select(r => Check(r))
                .Average(c => c ? 1f : 0f);
            
            string conditionString = string.Join("", Conditions.Select(c => $"{c.Operator}{c.Value}"));
            return new DiceResult(chanceOfSuccess, $"1d{dice}{conditionString} chance: {chanceOfSuccess * 100f}%");
        }
        
        bool succeeded = Check(total);
        float newTotal = succeeded ? 1f : 0f;
        return new DiceResult(newTotal, succeeded ? "Success(1)" : "Failure(0)");
    }
    
    private bool Check(float total)
    {
        return Conditions.All(c => c.Operator switch
        {
            "<" => total < c.Value,
            "<=" => total <= c.Value,
            ">" => total > c.Value,
            ">=" => total >= c.Value,
            "=" => total == c.Value,
            "=!" => total != c.Value,
            _ => throw new NotSupportedException($"Conditional operator '{c.Operator}' is not supported.")
        });
    }

    public readonly record struct Condition(string Operator, int Value);
}