using System.Globalization;

namespace Dice;

public interface IDice
{
    public int Sides { get; }
    public int Max => SideValues.Max();
    public int Min => SideValues.Min();
    
    public IEnumerable<int> SideValues { get; }
    public Func<float, string> Format { get; }

    public int Roll();
    
    public static string DefaultFormat(float v) => v.ToString(CultureInfo.InvariantCulture);
}

/// <param name="Min">Inclusive</param>
/// <param name="Max">Inclusive</param>
public readonly record struct DiceRange(int Min, int Max, Func<float, string> Format) : IDice
{
    public int Sides => Max - Min + 1;
    public IEnumerable<int> SideValues => Enumerable.Range(Min, Sides);

    public int Roll() => Random.Shared.Next(Min, Max + 1);

    public override string ToString() => Min == 1 ? $"{Max}" : $"[{Min}, {Min}]";
}

public readonly record struct DiceValues(IEnumerable<int> SideValues, Func<float, string> Format) : IDice
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
