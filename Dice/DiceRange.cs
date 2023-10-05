namespace Dice;

/// <param name="Min">Inclusive</param>
/// <param name="Max">Inclusive</param>
public readonly record struct DiceRange(int Min, int Max)
{
    public static implicit operator DiceRange((int Min, int Max) tuple) => new(tuple.Min, tuple.Max);
    
    public int Sides => Max - Min + 1;
}