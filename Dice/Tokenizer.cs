namespace Dice;

public class Tokenizer
{
    private readonly Queue<IToken> _tokens = new();

    public Queue<IToken> Tokenize(string expression)
    {
        var chars = expression.Reverse().ToStack();

        while (chars.TryPeek(out char c))
        {
            switch (c)
            {
                case var _ when IsSkippable(c):
                    chars.Pop();
                    break;
                
                case var _ when char.IsDigit(c):
                    ParseNumber();
                    break;

                case var _ when IsDiceOperator(c, chars):
                    _tokens.Enqueue(new DiceToken());
                    chars.Pop();
                    break;

                case Tokens.ADD or Tokens.SUB or Tokens.MUL or Tokens.DIV or Tokens.MOD:
                    _tokens.Enqueue(new OperatorToken(chars.Pop()));
                    break;
                
                case var _ when Tokens.KEEP.Contains(c):
                    _tokens.Enqueue(new KeepToken(c));
                    chars.Pop();
                    break;
                
                case var _ when IsDropOperator(c, chars):
                    _tokens.Enqueue(new DropToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.HIGHEST.Contains(c):
                    _tokens.Enqueue(new HighestToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.LOWEST.Contains(c):
                    _tokens.Enqueue(new LowestToken(c));
                    chars.Pop();
                    break;
            }

            continue;

            void ParseNumber()
            {
                string number = string.Empty;

                while (chars.TryPeek(out char c) && char.IsDigit(c))
                    number += chars.Pop();

                _tokens.Enqueue(new NumberToken(int.Parse(number)));
            }
        }

        return _tokens;
    }

    private static bool IsSkippable(char c) => c is ' ' or '\t' or '\n' or '\r';
    
    private static bool IsDiceOperator(char c, Stack<char> chars)
    {
        if (!Tokens.DICE_OPERATORS.Contains(c))
            return false;

        chars.Pop();
        
        if (!chars.TryPeek(out char next))
        {
            chars.Push(c);
            return true;
        }
        
        chars.Push(c);
        return !Tokens.HIGHEST.Contains(next) && !Tokens.LOWEST.Contains(next);
    }

    private static bool IsDropOperator(char c, Stack<char> chars)
    {
        if (!Tokens.DROP.Contains(c))
            return false;

        chars.Pop();
        
        if (!chars.TryPeek(out char next))
        {
            chars.Push(c);
            return false;
        }
        
        chars.Push(c);
        return Tokens.HIGHEST.Contains(next) || Tokens.LOWEST.Contains(next);
    }
}

public interface IToken
{
    public string Value { get; }
}

public record NumberToken(int Number) : IToken
{
    public string Value => Number.ToString();
}

public record DiceToken : IToken
{
    public string Value => "d";
}

public record OperatorToken(char Operator) : IToken
{
    public string Value => Operator.ToString();
}

public record KeepToken(char Symbol) : IToken
{
    public string Value => Symbol.ToString();
}

public record DropToken(char Symbol) : IToken
{
    public string Value => Symbol.ToString();
}

public record HighestToken(char Symbol) : IToken
{
    public string Value => Symbol.ToString();
}

public record LowestToken(char Symbol) : IToken
{
    public string Value => Symbol.ToString();
}

public static class EnumerableExtensions
{
    public static Stack<T> ToStack<T>(this IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return new Stack<T>(collection);
    }
}

public static class Tokens
{
    public static readonly char[] DICE_OPERATORS = { 'd', 'D' };
    public static readonly char[] KEEP = { 'k', 'K' };
    public static readonly char[] DROP = { 'd', 'D' };
    public static readonly char[] HIGHEST = { 'h', 'H' };
    public static readonly char[] LOWEST = { 'l', 'L' };
    
    public const char ADD = '+';
    public const char SUB = '-';
    public const char MUL = '*';
    public const char DIV = '/';
    public const char MOD = '%';
    // public const char OPEN_PAREN = '(';
    // public const char CLOSE_PAREN = ')';
}