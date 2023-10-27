namespace Dice;

public class Tokenizer
{
    private readonly Queue<IToken> _tokens = new();

    public Queue<IToken> Tokenize(string expression)
    {
        Stack<char> chars = expression.Reverse().ToStack();

        while (chars.TryPeek(out char c))
        {
            switch (c)
            {
                case var _ when IsSkippable(c):
                    chars.Pop();
                    break;
                
                case var _ when char.IsDigit(c):
                    ParseNumber(chars);
                    break;

                case var _ when IsDiceOperator(c, chars):
                    _tokens.Enqueue(new DiceToken(c));
                    chars.Pop();
                    break;

                case Tokens.ADD or Tokens.SUB or Tokens.MUL or Tokens.DIV or Tokens.MOD:
                    _tokens.Enqueue(new OperatorToken(chars.Pop()));
                    break;
                
                case Tokens.EXPLODE:
                    _tokens.Enqueue(new ExplodeToken());
                    chars.Pop();
                    break;
                
                case Tokens.OPEN_PAREN:
                    _tokens.Enqueue(new OpenParenToken());
                    chars.Pop();
                    break;
                
                case Tokens.CLOSE_PAREN:
                    _tokens.Enqueue(new CloseParenToken());
                    chars.Pop();
                    break;
                
                case Tokens.OPEN_BRACKET:
                    _tokens.Enqueue(new OpenBracketToken());
                    chars.Pop();
                    break;
                
                case Tokens.CLOSE_BRACKET:
                    _tokens.Enqueue(new CloseBracketToken());
                    chars.Pop();
                    break;
                
                case Tokens.DELIMITER:
                    _tokens.Enqueue(new DelimiterToken());
                    chars.Pop();
                    break;
                
                case Tokens.OR:
                    _tokens.Enqueue(new OrToken());
                    chars.Pop();
                    break;

                case var _ when Tokens.Keep.Contains(c):
                    _tokens.Enqueue(new KeepToken(c));
                    chars.Pop();
                    break;
                
                case var _ when IsDropOperator(c, chars):
                    _tokens.Enqueue(new DropToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.Highest.Contains(c):
                    _tokens.Enqueue(new HighestToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.Lowest.Contains(c):
                    _tokens.Enqueue(new LowestToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.Infinite.Contains(c):
                    _tokens.Enqueue(new InfiniteToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.ReRoll.Contains(c):
                    _tokens.Enqueue(new ReRollToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.FudgeOrFate.Contains(c):
                    _tokens.Enqueue(new FudgeFateToken(c));
                    chars.Pop();
                    break;
                
                case var _ when Tokens.Unique.Contains(c):
                    _tokens.Enqueue(new UniqueToken(c));
                    chars.Pop();
                    break;
                
                default:
                    HandleMultilineTokens(chars);
                    break;
            }
        }

        return _tokens;
    }

    private void HandleMultilineTokens(Stack<char> chars)
    {
        var c = chars.Pop();
        switch (c)
        {
            case var _ when c.ToString() == Tokens.LessThan:
            {
                if (chars.TryPeek(out char next) && string.Concat(c, next) == Tokens.LessThanOrEqual)
                {
                    chars.Pop();
                    _tokens.Enqueue(new ConditionToken(Tokens.LessThanOrEqual));
                    break;
                }

                _tokens.Enqueue(new ConditionToken(Tokens.LessThan));
                break;
            }
            
            case var _ when c.ToString() == Tokens.GreaterThan:
            {
                if (chars.TryPeek(out char next) && string.Concat(c, next) == Tokens.GreaterThanOrEqual)
                {
                    chars.Pop();
                    _tokens.Enqueue(new ConditionToken(Tokens.GreaterThanOrEqual));
                    break;
                }

                _tokens.Enqueue(new ConditionToken(Tokens.GreaterThan));
                break;
            }
            
            case var _ when c.ToString() == Tokens.Equal:
            {
                if (chars.TryPeek(out char next) && string.Concat(c, next) == Tokens.NotEqual)
                {
                    chars.Pop();
                    _tokens.Enqueue(new ConditionToken(Tokens.NotEqual));
                    break;
                }

                _tokens.Enqueue(new ConditionToken(Tokens.Equal));
                break;
            }
            
            default:
                throw new InvalidDataException($"Unexpected symbol: {c}");
        }
    }

    private void ParseNumber(Stack<char> chars)
    {
        string number = string.Empty;
                
        while (chars.TryPeek(out char c) && char.IsDigit(c))
            number += chars.Pop();

        _tokens.Enqueue(new NumberToken(int.Parse(number)));
    }

    private static bool IsSkippable(char c) => c is ' ' or '\t' or '\n' or '\r';
    
    private static bool IsDiceOperator(char c, Stack<char> chars)
    {
        if (!Tokens.Die.Contains(c))
            return false;

        chars.Pop();
        
        if (!chars.TryPeek(out char next))
        {
            chars.Push(c);
            return true;
        }
        
        chars.Push(c);
        return !Tokens.Highest.Contains(next) && !Tokens.Lowest.Contains(next);
    }

    private static bool IsDropOperator(char c, Stack<char> chars)
    {
        if (!Tokens.Drop.Contains(c))
            return false;

        chars.Pop();
        
        if (!chars.TryPeek(out char next))
        {
            chars.Push(c);
            return false;
        }
        
        chars.Push(c);
        return Tokens.Highest.Contains(next) || Tokens.Lowest.Contains(next);
    }
}

public interface IToken { }

public record NumberToken(int Number) : IToken
{
    public override string ToString() => Number.ToString();
}

public record DiceToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record OperatorToken(char Operator) : IToken
{
    public override string ToString() => Operator.ToString();
}

public record KeepToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record DropToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record HighestToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record LowestToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record ExplodeToken : IToken
{
    public override string ToString() => Tokens.EXPLODE.ToString();
}

public record OpenParenToken : IToken
{
    public override string ToString() => Tokens.OPEN_PAREN.ToString();
}

public record CloseParenToken : IToken
{
    public override string ToString() => Tokens.CLOSE_PAREN.ToString();
}

public record OpenBracketToken : IToken
{
    public override string ToString() => Tokens.OPEN_BRACKET.ToString();
}

public record CloseBracketToken : IToken
{
    public override string ToString() => Tokens.CLOSE_BRACKET.ToString();
}

public record DelimiterToken : IToken
{
    public override string ToString() => Tokens.DELIMITER.ToString();
}

public record OrToken : IToken
{
    public override string ToString() => Tokens.OR.ToString();
}

public record InfiniteToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record ReRollToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record ConditionToken(string ConditionalOperator) : IToken
{
    public override string ToString() => ConditionalOperator;
    public Func<float, bool> GetCondition(int checkValue) => ConditionalOperator switch
    {
        var c when c == Tokens.LessThan => v => v < checkValue,
        var c when c == Tokens.LessThanOrEqual => v => v <= checkValue,
        var c when c == Tokens.GreaterThan => v => v > checkValue,
        var c when c == Tokens.GreaterThanOrEqual => v => v >= checkValue,
        var c when c == Tokens.Equal => v => v == checkValue,
        var c when c == Tokens.NotEqual => v => v != checkValue,
        _ => throw new InvalidDataException($"Unexpected conditional operator: {ConditionalOperator}")
    };
}

public record FudgeFateToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
}

public record UniqueToken(char Symbol) : IToken
{
    public override string ToString() => Symbol.ToString();
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
    public static readonly char[] Die = { 'd', 'D' };
    public static readonly char[] Keep = { 'k', 'K' };
    public static readonly char[] Drop = { 'd', 'D' };
    public static readonly char[] Highest = { 'h', 'H' };
    public static readonly char[] Lowest = { 'l', 'L' };
    public static readonly char[] Infinite = { 'i', 'I' };
    public static readonly char[] ReRoll = { 'r', 'R' };
    public static readonly char[] FudgeOrFate = { 'f', 'F' };
    public static readonly char[] Unique = { 'u', 'U' };
    
    public const char EXPLODE = '!';
    
    public const char ADD = '+';
    public const char SUB = '-';
    public const char MUL = '*';
    public const char DIV = '/';
    public const char MOD = '%';
    
    public static readonly string LessThan = "<";
    public static readonly string GreaterThan = ">";
    public static readonly string LessThanOrEqual = "<=";
    public static readonly string GreaterThanOrEqual = ">=";
    public static readonly string Equal = "=";
    public static readonly string NotEqual = "=!";
    
    public const char OR = '\\';
    
    public const char OPEN_PAREN = '(';
    public const char CLOSE_PAREN = ')';
    public const char OPEN_BRACKET = '[';
    public const char CLOSE_BRACKET = ']';
    
    public const char DELIMITER = ',';
}