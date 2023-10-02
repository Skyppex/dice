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
                case var _ when char.IsDigit(c):
                    ParseNumber();
                    break;
                
                case Tokens.d or Tokens.D:
                    _tokens.Enqueue(new DiceToken());
                    chars.Pop();
                    break;
                
                case Tokens.ADD or Tokens.SUB or Tokens.MUL or Tokens.DIV or Tokens.MOD:
                    _tokens.Enqueue(new OperatorToken(chars.Pop()));
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
    public string Value => Tokens.d.ToString();
}

public record OperatorToken(char Operator) : IToken
{
    public string Value => Operator.ToString();
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
    public const char d = 'd';
    public const char D = 'D';
    public const char ADD = '+';
    public const char SUB = '-';
    public const char MUL = '*';
    public const char DIV = '/';
    public const char MOD = '%';
    // public const char OPEN_PAREN = '(';
    // public const char CLOSE_PAREN = ')';
}