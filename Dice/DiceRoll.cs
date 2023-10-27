using Monads;
using static Monads.Option;

namespace Dice;

public readonly record struct DiceRoll(IDice Dice, params IRollModifier[] RollModifiers)
{
    public DiceResult Roll(IDiceRollHandlers handler, IReadOnlyList<float> previousResults)
    {
        List<DiceResult> diceResults = new();

        float total = handler.Handle(Dice);

        IDice diceToUse = Dice;
        
        foreach (IRollModifier t in RollModifiers)
        {
            Option<DiceResult> diceResult = t.Modify(total, diceToUse, handler, previousResults, out diceToUse);
            
            diceResult.Match(
                some: dr =>
                {
                    diceResults.Add(dr);
                    total = dr.Value;
                },
                none: () => { });
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
    public Option<DiceResult> Modify(float total, IDice dice, IDiceRollHandlers handler, IReadOnlyList<float> previousResults, out IDice newDice);
}

public record ExplodeModifier(int MaxExplosions = 1, bool Combined = false) : IRollModifier
{
    public Option<DiceResult> Modify(float total, IDice dice, IDiceRollHandlers handler, IReadOnlyList<float> previousResults, out IDice newDice)
    {
        newDice = dice;
        List<float> rolls = new() { total };
        Option<float> newTotal = ModifyRecurse(total, dice, total, rolls, handler);
        
        if (Combined)
            return newTotal.Map(t => new DiceResult(t, $"{dice.Format(t)}{(rolls.Count > 1 ? new string('!', rolls.Count - 1) : string.Empty)}"));
            
        return rolls.Count <= 1
            ? newTotal.Map(t => new DiceResult(t, dice.Format(t)))
            : newTotal.Map(t => new DiceResult(t, $"({string.Join(", ", rolls.Select(r => $"{dice.Format(r)}!").Take(rolls.Count - 1))}, {rolls.Last()})"));
    }

    private Option<float> ModifyRecurse(
        float total,
        IDice dice,
        float previousRoll,
        List<float> rolls,
        IDiceRollHandlers handler)
    {
        if (MaxExplosions is 0)
            return Some(total);
        
        if (previousRoll >= dice.Max)
            return HandleExplode(1f);

        if (handler.ExhaustiveRoll)
            return HandleExplode(MathF.Pow(1f / dice.Sides, rolls.Count));

        return rolls.Count == 1 ? None<float>() : Some(total);

        Option<float> HandleExplode(float multiplier)
        {
            float explosion = multiplier * handler.Handle(dice);
            rolls.Add(explosion);
            total += explosion;

            return new ExplodeModifier(MaxExplosions - 1)
                .ModifyRecurse(total, dice, explosion, rolls, handler);
        }
    }
}

public record ReRollModifier(IRollModifier[] RollModifiers, int MaxReRolls = 1, Func<float, bool>? Condition = null) : IRollModifier
{
    public Option<DiceResult> Modify(float total, IDice dice, IDiceRollHandlers handler, IReadOnlyList<float> previousResults, out IDice newDice)
    {
        newDice = dice;
        List<float> rolls = new() { total };
        List<string> builder = new();
        Option<float> newTotal = ModifyRecurse(total, dice, total, rolls, handler, previousResults, builder);
        
        return newTotal.Map(t =>
        {
            var formattedLastRoll = dice.Format(rolls.Last());
            var suffixExpression = () => builder.Count > 1 ? $"({string.Join(" <- ", builder)})" : string.Empty;
            var suffix = rolls.Count > 1 ? $"r{suffixExpression()}" : string.Empty;
            return new DiceResult(t, $"{formattedLastRoll}{suffix}");
        });
    }

    private Option<float> ModifyRecurse(
        float total,
        IDice dice,
        float previousRoll,
        List<float> rolls,
        IDiceRollHandlers handler,
        IReadOnlyList<float> previousResults,
        List<string> builder)
    {
        if (MaxReRolls is 0)
            return Some(total);
        
        if (Condition?.Invoke(previousRoll) ?? previousRoll == dice.Min)
            return HandleReRoll();

        if (handler.ExhaustiveRoll)
            throw new NotSupportedException("Exhaustive roll is not supported for re-rolls.");

        return rolls.Count == 1 ? None<float>() : Some(total);

        Option<float> HandleReRoll()
        {
            float reRoll = handler.Handle(dice);
            total = reRoll;

            IDice diceToUse = dice;
            List<DiceResult> diceResults = new();
            
            foreach (IRollModifier rollModifier in RollModifiers)
            {
                Option<DiceResult> option =  rollModifier.Modify(total, diceToUse, handler, previousResults, out diceToUse);

                option.Match(
                    some: dr =>
                    {
                        total = dr.Value;
                        diceResults.Add(dr);
                    },
                    none: () => { });
            }

            rolls.Add(total);

            if (diceResults.Count > 1)
            {
                string expression = diceResults.Count switch
                {
                    > 1 => $"{string.Join(" <- ", diceResults.Select(dr => dr.Expression))}",
                    1 => diceResults.Single().Expression,
                    _ => diceToUse.Format(total),
                };
                
                builder.Insert(0, expression);
            }
            
            return (this with { MaxReRolls = MaxReRolls - 1 })
                .ModifyRecurse(total, dice, total, rolls, handler, previousResults, builder);
        }
    }
}

public record UniqueModifier(IRollModifier[] RollModifiers, int MaxRetries = 100) : IRollModifier
{
    public Option<DiceResult> Modify(float total, IDice dice, IDiceRollHandlers handler, IReadOnlyList<float> previousResults, out IDice newDice)
    {
        newDice = dice;
        
        for (int i = 0; i < MaxRetries; i++)
        {
            if (!previousResults.Contains(total))
                return Some(new DiceResult(total, $"{dice.Format(total)}{(i > 0 ? 'u' : string.Empty)}"));
            
            total = handler.Handle(dice);

            foreach (IRollModifier rollModifier in RollModifiers)
            {
                Option<DiceResult> option = rollModifier.Modify(total, dice, handler, previousResults, out newDice);

                option.Match(
                    some: dr => total = dr.Value,
                    none: () => { });
            }
        }

        throw new OverflowException($"Could not find a unique roll in {MaxRetries} tries.");
    }
}

public record ConditionModifier(List<ConditionModifier.Condition> Conditions) : IRollModifier
{
    public Option<DiceResult> Modify(float total, IDice dice, IDiceRollHandlers handler, IReadOnlyList<float> previousResults, out IDice newDice)
    {
        newDice = new DiceValues(new[] {0, 1}, IDice.DefaultFormat);

        if (handler.ExhaustiveRoll)
        {
            float chanceOfSuccess = Enumerable.Range(dice.Min, dice.Sides)
                .Select(r => Check(r))
                .Average(c => c ? 1f : 0f);
            
            string conditionString = string.Join("", Conditions.Select(c => $"{c.Operator}{c.Value}"));
            return Some(new DiceResult(chanceOfSuccess, $"1d{dice}{conditionString} chance: {chanceOfSuccess * 100f}%"));
        }
        
        bool succeeded = Check(total);
        float newTotal = succeeded ? 1f : 0f;
        return Some(new DiceResult(newTotal, succeeded ? "Success(1)" : "Failure(0)"));
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