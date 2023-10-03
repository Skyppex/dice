namespace Dice;

public class Parser
{
    private readonly Queue<IToken> _tokens;

    private Parser(Queue<IToken> tokens) => _tokens = tokens;

    public static IExpression Parse(Queue<IToken> tokens)
    {
        var parser = new Parser(tokens);
        return parser.ParseExpression();
    }

    private IExpression ParseExpression() => ParseAdditive();

    private IExpression ParseAdditive()
    {
        IExpression left = ParseMultiplicative();

        while (_tokens.TryPeek(out IToken? nextToken) && nextToken is OperatorToken operatorToken)
        {
            if (operatorToken.Operator is not (Tokens.ADD or Tokens.SUB))
                continue;
            
            _tokens.Dequeue();
            IExpression right = ParseMultiplicative();
            left = new BinaryExpression(left, operatorToken.Operator, right);
        }

        return left;
    }

    private IExpression ParseMultiplicative()
    {
        IExpression left = ParsePrimary();

        while (_tokens.TryPeek(out IToken? nextToken) && nextToken is OperatorToken operatorToken)
        {
            if (operatorToken.Operator is not (Tokens.MUL or Tokens.DIV or Tokens.MOD))
                return left;
            
            _tokens.Dequeue();
            IExpression right = ParsePrimary();
            left = new BinaryExpression(left, operatorToken.Operator, right);
        }

        return left;
    }
    
    private IExpression ParsePrimary()
    {
        if (!_tokens.TryDequeue(out IToken? token))
            throw new Exception("Unexpected end of input");

        switch (token)
        {
            case NumberToken numberToken:
            {
                if (_tokens.TryPeek(out IToken? nextToken) && nextToken is DiceToken)
                {
                    return ParseDice(numberToken);
                }
                
                return new NumberExpression(numberToken.Number);
            }
            
        }
        
        throw new Exception($"Unexpected token: {token.Value}");
    }

    private IExpression ParseDice(NumberToken numberToken)
    {
        _tokens.Dequeue();

        var secondNumberToken = Expect<NumberToken>("Expected number after d");

        if (!_tokens.TryPeek(out IToken? nextToken) || nextToken is not (KeepToken or DropToken))
            return new DiceExpression(numberToken.Number, secondNumberToken.Number, DiceExpression.Modes.Default);
        
        _tokens.Dequeue();

        switch (nextToken)
        {
            case KeepToken:
            {
                if (_tokens.TryPeek(out IToken? nextNextToken))
                {
                    switch (nextNextToken)
                    {
                        case LowestToken:
                        {
                            _tokens.Dequeue();
                            var amount = Expect<NumberToken>("Expected number after k");
                            return new DiceExpression(numberToken.Number, secondNumberToken.Number, DiceExpression.Modes.KeepLowest(amount.Number));
                        }
                        
                        case HighestToken:
                        {
                            _tokens.Dequeue();
                            var amount = Expect<NumberToken>("Expected number after k");
                            return new DiceExpression(numberToken.Number, secondNumberToken.Number, DiceExpression.Modes.KeepHighest(amount.Number));
                        }
                        
                        default:
                        {
                            var amount = Expect<NumberToken>("Expected number after k");
                            return new DiceExpression(numberToken.Number, secondNumberToken.Number, DiceExpression.Modes.KeepHighest(amount.Number));
                        }
                    }
                }

                break;
            }
                
            case DropToken:
            {
                if (_tokens.TryPeek(out IToken? nextNextToken))
                {
                    switch (nextNextToken)
                    {
                        case HighestToken:
                        {
                            _tokens.Dequeue();
                            var amount = Expect<NumberToken>("Expected number after dh");
                            return new DiceExpression(numberToken.Number, secondNumberToken.Number, DiceExpression.Modes.DropHighest(amount.Number));
                        }
                        case LowestToken:
                        {
                            _tokens.Dequeue();
                            var amount = Expect<NumberToken>("Expected number after dl");
                            return new DiceExpression(numberToken.Number, secondNumberToken.Number, DiceExpression.Modes.DropLowest(amount.Number));
                        }
                        
                        default:
                            throw new Exception($"Unexpected token: {nextNextToken.Value}. Expected h or l after d when choosing to drop dice");
                    }
                }

                break;
            }
        }

        return new DiceExpression(numberToken.Number, secondNumberToken.Number, DiceExpression.Modes.Default);
    }

    private TToken Expect<TToken>(string errorMessage) where TToken : IToken
    {
        if (_tokens.TryPeek(out IToken? nextToken) && nextToken is not TToken)
            throw new Exception($"Unexpected token: {nextToken.Value}, expected {typeof(TToken).Name} | {errorMessage}");

        return (TToken)_tokens.Dequeue();
    }
}

public interface IExpression
{
    public string Symbol { get; }

    public DiceResult Evaluate();
}

public record DiceExpression(int Amount, int Dice, DiceExpression.IMode Mode) : IExpression
{
    public string Symbol => $"{Amount}d{Dice}{Mode.Symbol}";
    
    public DiceResult Evaluate() => Mode.Evaluate(Amount, Dice);

    public static class Modes
    {
        public static IMode Default => new DefaultMode();
        public static IMode KeepHighest(int amount) => new KeepHighestMode(amount);
        public static IMode KeepLowest(int amount) => new KeepLowestMode(amount);
        public static IMode DropHighest(int amount) => new DropHighestMode(amount);
        public static IMode DropLowest(int amount) => new DropLowestMode(amount);
    }

    public interface IMode
    {
        public DiceResult Evaluate(int amount, int dice);
        public string Symbol { get; }
    }

    private record DefaultMode : IMode
    {
        public DiceResult Evaluate(int amount, int dice)
        {
            var rand = Random.Shared;

            var rolls = Enumerable.Range(0, amount)
                .Select(_ => rand.Next(1, dice + 1))
                .ToArray();

            int total = rolls.Sum();
            
            return new DiceResult(total, $"[{string.Join(", ", rolls)}]");
        }

        public string Symbol => string.Empty;
    }

    private record KeepHighestMode(int Amount) : IMode
    {
        public DiceResult Evaluate(int amount, int dice)
        {
            var rand = Random.Shared;

            var rolls = Enumerable.Range(0, amount)
                .Select(_ => rand.Next(1, dice + 1))
                .OrderByDescending(n => n)
                .ToArray();
            
            int total = rolls
                .Take(Amount)
                .Sum();

            return new DiceResult(total, $"[{string.Join(", ", rolls.Take(Amount))}, {string.Join(", ", rolls.Skip(Amount).Select(r => r + "d"))}]");
        }

        public string Symbol => $"kh{Amount}";
    }

    private record KeepLowestMode(int Amount) : IMode
    {
        public DiceResult Evaluate(int amount, int dice)
        {
            var rand = Random.Shared;
            
            var rolls = Enumerable.Range(0, amount)
                .Select(_ => rand.Next(1, dice + 1))
                .OrderBy(n => n)
                .ToArray();
            
            int total = rolls
                .Take(Amount)
                .Sum();

            return new DiceResult(total, $"[{string.Join(", ", rolls.Take(Amount))}, {string.Join(", ", rolls.Skip(Amount).Select(r => r + "d"))}]");
        }

        public string Symbol => $"kl{Amount}";
    }

    private record DropHighestMode(int Amount) : IMode
    {
        public DiceResult Evaluate(int amount, int dice)
        {
            var rand = Random.Shared;
            
            var rolls = Enumerable.Range(0, amount)
                .Select(_ => rand.Next(1, dice + 1))
                .OrderByDescending(n => n)
                .ToArray();

            int total = rolls
                .Skip(Amount)
                .Sum();

            return new DiceResult(total, $"[{string.Join(", ", rolls.Take(Amount).Select(r => r + "d"))}, {string.Join(", ", rolls.Skip(Amount))}]");
        }

        public string Symbol => $"dh{Amount}";
    }

    private record DropLowestMode(int Amount) : IMode
    {
        public DiceResult Evaluate(int amount, int dice)
        {
            var rand = Random.Shared;
            
            var rolls = Enumerable.Range(0, amount)
                .Select(_ => rand.Next(1, dice + 1))
                .OrderBy(n => n)
                .ToArray();

            int total = rolls
                .Skip(Amount)
                .Sum(_ => rand.Next(1, dice + 1));

            return new DiceResult(total, $"[{string.Join(", ", rolls.Take(Amount).Select(r => r + "d"))}, {string.Join(", ", rolls.Skip(Amount))}]");
        }

        public string Symbol => $"dl{Amount}";
    }
}

public record NumberExpression(int Number) : IExpression
{
    public string Symbol => Number.ToString();
    
    public DiceResult Evaluate() => new(Number, Number.ToString());
}

public record BinaryExpression(IExpression Left, char Operator, IExpression Right) : IExpression
{
    public string Symbol => Operator.ToString();
    
    public DiceResult Evaluate()
    {
        var leftResult = Left.Evaluate();
        var rightResult = Right.Evaluate();
        
        var result = Operator switch
        {
            Tokens.ADD => leftResult.Result + rightResult.Result,
            Tokens.SUB => leftResult.Result - rightResult.Result,
            Tokens.MUL => leftResult.Result * rightResult.Result,
            Tokens.DIV => leftResult.Result / rightResult.Result,
            Tokens.MOD => leftResult.Result % rightResult.Result,
            _ => throw new Exception($"Unexpected operator: {Operator}")
        };

        return new DiceResult(result, $"{leftResult.Expression} {Operator} {rightResult.Expression}");
    }
}