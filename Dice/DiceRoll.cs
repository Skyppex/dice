namespace Dice;

public record struct DiceRoll(int Sides, params IRollModifier[] RollModifiers)
{
    public DiceResultInt Roll(Random rand)
    {
        List<int> rolls = new();
        List<DiceResultInt> diceResults = new();

        int total = rand.Next(1, Sides + 1);
        rolls.Add(total);

        foreach (IRollModifier t in RollModifiers)
        {
            DiceResultInt diceResult = t.Modify(total, Sides, rolls, rand);
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
    public DiceResultInt Modify(int total, int sides, List<int> rolls, Random rand);
}

public record ExplodeModifier(int MaxExplosions = 1) : IRollModifier
{
    public DiceResultInt Modify(int total, int sides, List<int> rolls, Random rand)
    {
        int newTotal = ModifyRecurse(total, sides, total, rolls, rand);

        return rolls.Count <= 1
            ? new DiceResultInt(newTotal, $"{rolls.Single()}")
            : new DiceResultInt(newTotal, $"({string.Join(", ", rolls.Select(r => $"{r}!").Take(rolls.Count - 1))}, {rolls.Last()})");
    }

    private int ModifyRecurse(int total, int sides, int previousRoll, List<int> rolls, Random rand)
    {
        if (MaxExplosions is 0)
            return total;
        
        if (previousRoll == sides)
        {
            int explosion = rand.Next(1, sides + 1);
            total += explosion;
            rolls.Add(explosion);
            return new ExplodeModifier(MaxExplosions - 1).ModifyRecurse(total, sides, explosion, rolls, rand);
        }

        return total;
    }
}