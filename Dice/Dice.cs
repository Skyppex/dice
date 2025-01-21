using System.Globalization;

namespace Dice;

public interface IDice
{
    public int Sides { get; }
    public int Max => SideValues.Max();
    public int Min => SideValues.Min();
    public int Count { get; }
    public IEnumerable<int> SideValues { get; }
    public Func<float, string> Format { get; }

    public int Roll();

    public static string DefaultFormat(float v) => v.ToString(CultureInfo.InvariantCulture);
}

/// <param name="Min">Inclusive</param>
/// <param name="Max">Inclusive</param>
public readonly record struct DiceRange(int Min, int Max, int Count, Func<float, string> Format)
    : IDice
{
    public int Sides => Max - Min + 1;
    public IEnumerable<int> SideValues => Enumerable.Range(Min, Sides);

    public int Roll()
    {
        var min = Min;
        var max = Max;

        return Enumerable.Range(0, Count).Select(_ => Random.Shared.Next(min, max + 1)).Sum();
    }

    public override string ToString() => Min == 1 ? $"{Max}" : $"[{Min}, {Min}]";
}

public readonly record struct DiceValues(
    IEnumerable<int> SideValues,
    int Count,
    Func<float, string> Format
) : IDice
{
    public int Sides => SideValues.Count();
    public int Min => SideValues.Min();
    public int Max => SideValues.Max();

    public int Roll()
    {
        var countSides = SideValues.Count();
        var sideValues = SideValues;

        return Enumerable
            .Range(0, Count)
            .Select(_ => Random.Shared.Next(0, countSides))
            .Sum(i => sideValues.ElementAt(i));
    }

    public override string ToString() =>
        $"[{string.Join("|", SideValues.Select(v => v.ToString()))}]";
}
