namespace Dice;

public interface IDice
{
    public int Sides { get; }
    
    public int Max => SideValues.Max();
    public int Min => SideValues.Min();
    
    public IEnumerable<int> SideValues { get; }

    public int Roll();
}

/// <param name="Min">Inclusive</param>
/// <param name="Max">Inclusive</param>
public readonly record struct DiceRange(int Min, int Max) : IDice
{
    public static implicit operator DiceRange((int Min, int Max) tuple) => new(tuple.Min, tuple.Max);
    
    public int Sides => Max - Min + 1;
    public IEnumerable<int> SideValues => Enumerable.Range(Min, Sides);

    public int Roll() => Random.Shared.Next(Min, Max + 1);

    public override string ToString() => Min == 1 ? $"{Max}" : $"[{Min}, {Min}]";
}

public readonly record struct DiceValues(IEnumerable<int> SideValues) : IDice
{
    public int Sides => SideValues.Count();
    public int Min => SideValues.Min();
    public int Max => SideValues.Max();
    
    public int Roll()
    {
        int index = Random.Shared.Next(0, SideValues.Count());
        return SideValues.ElementAt(index);
    }

    public override string ToString() => $"[{string.Join("|", SideValues.Select(v => v.ToString()))}]";
}
