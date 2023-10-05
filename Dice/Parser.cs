namespace Dice;

public class Parser
{
    private readonly Queue<IToken> _tokens;

    private Parser(Queue<IToken> tokens) => _tokens = tokens;

    public static IExpression Parse(Queue<IToken> tokens)
    {
        var parser = new Parser(tokens);
        var expression = parser.ParseExpression();
        
        if (parser._tokens.Count != 0)
            Console.WriteLine($"Found unexpected tokens at the end of the expression: {string.Join("", parser._tokens.Select(t => t.Value))}\n" +
                              $"Will evaluate excluding the unexpected tokens.");
        
        return expression;
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

        while (_tokens.TryPeek(out IToken? nextToken) &&
               nextToken is OperatorToken operatorToken &&
               operatorToken.Operator is Tokens.MUL or Tokens.DIV)
        {
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
                    return ParseDice(numberToken);

                return new NumberExpression(numberToken.Number);
            }

            case OpenParenToken:
            {
                IExpression expression = ParseExpression();
                Expect<CloseParenToken>("Expected )");
                return new ParenExpression(expression);
            }
        }

        throw new Exception($"Unexpected token: {token.Value}");
    }

    private IExpression ParseDice(NumberToken numberToken)
    {
        _tokens.Dequeue();

        var secondNumberToken = Expect<NumberToken>("Expected number after d");
        var diceRange = (1, secondNumberToken.Number);

        List<IRollModifier> rollModifiers = ParseRollModifiers();
        
        if (!_tokens.TryPeek(out IToken? nextToken))
            return new DiceExpression(numberToken.Number, diceRange, DiceExpression.Modes.Default(rollModifiers.ToArray()));

        switch (nextToken)
        {
            case KeepToken:
            {
                _tokens.Dequeue();
                return ParseKeep(numberToken, diceRange, rollModifiers);
            }

            case DropToken:
            {
                _tokens.Dequeue();

                if (ParseDrop(numberToken, diceRange, rollModifiers, out IExpression? dropExpression))
                    return dropExpression!;

                break;
            }
        }

        return new DiceExpression(numberToken.Number, diceRange, DiceExpression.Modes.Default(rollModifiers.ToArray()));
    }

    private List<IRollModifier> ParseRollModifiers()
    {
        List<IRollModifier> rollModifiers = new();
        
        while (_tokens.TryPeek(out IToken? nextToken))
        {
            switch (nextToken)
            {
                case ExplodeToken:
                {
                    _tokens.Dequeue();

                    if (_tokens.TryPeek(out IToken? secondNextToken) && secondNextToken is NumberToken numberToken)
                    {
                        _tokens.Dequeue();
                        rollModifiers.Add(new ExplodeModifier(numberToken.Number));
                        break;
                    }

                    rollModifiers.Add(new ExplodeModifier());
                    break;
                }
                
                default:
                    return rollModifiers;
            }
        }

        return rollModifiers;
    }

    private IExpression ParseKeep(NumberToken numberToken, DiceRange diceRange, List<IRollModifier> rollModifiers)
    {
        if (_tokens.TryPeek(out IToken? nextNextToken))
        {
            switch (nextNextToken)
            {
                case LowestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    return new DiceExpression(numberToken.Number, diceRange,
                        DiceExpression.Modes.KeepLowest(amount.Number, rollModifiers.ToArray()));
                }

                case HighestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));


                    return new DiceExpression(numberToken.Number, diceRange,
                        DiceExpression.Modes.KeepHighest(amount.Number, rollModifiers.ToArray()));
                }

                default:
                {
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    return new DiceExpression(numberToken.Number, diceRange,
                        DiceExpression.Modes.KeepHighest(amount.Number, rollModifiers.ToArray()));
                }
            }
        }

        return new DiceExpression(numberToken.Number, diceRange,
            DiceExpression.Modes.KeepHighest(1, rollModifiers.ToArray()));
    }

    private bool ParseDrop(NumberToken numberToken, DiceRange diceRange, List<IRollModifier> rollModifiers, out IExpression? dropExpression)
    {
        if (_tokens.TryPeek(out IToken? nextNextToken))
        {
            switch (nextNextToken)
            {
                case HighestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    dropExpression = new DiceExpression(numberToken.Number, diceRange,
                        DiceExpression.Modes.DropHighest(amount.Number, rollModifiers.ToArray()));

                    return true;
                }

                case LowestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    dropExpression = new DiceExpression(numberToken.Number, diceRange,
                        DiceExpression.Modes.DropLowest(amount.Number, rollModifiers.ToArray()));

                    return true;
                }

                default:
                    throw new Exception(
                        $"Unexpected token: {nextNextToken.Value}. Expected h or l after d when choosing to drop dice");
            }
        }

        dropExpression = null;
        return false;
    }

    private TToken Expect<TToken>(string errorMessage) where TToken : IToken
    {
        if (!_tokens.TryPeek(out IToken? nextToken) || nextToken is not TToken)
            throw new Exception(errorMessage);

        return (TToken)_tokens.Dequeue();
    }

    private TToken ExpectOrDefault<TToken>(TToken defaultValue) where TToken : IToken
    {
        if (!_tokens.TryPeek(out IToken? nextToken) || nextToken is not TToken)
            return defaultValue;

        return (TToken)_tokens.Dequeue();
    }

    private TToken ExpectOrElse<TToken>(Func<TToken> defaultGetter) where TToken : IToken
    {
        ArgumentNullException.ThrowIfNull(defaultGetter, nameof(defaultGetter));
        
        if (!_tokens.TryPeek(out IToken? nextToken) || nextToken is not TToken)
            return defaultGetter();

        return (TToken)_tokens.Dequeue();
    }
}

public interface IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler);
}

public record DiceExpression(int Amount, DiceRange DiceRange, DiceExpression.IMode Mode) : IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler)
    {
        DiceResult diceResult = Mode.Evaluate(Amount, DiceRange, handler);
        return diceResult with { Expression = $"[{diceResult.Expression}]" };
    }

    public static class Modes
    {
        public static IMode Default(params IRollModifier[] rollModifiers) => new DefaultMode(rollModifiers);

        public static IMode KeepHighest(int amount, params IRollModifier[] rollModifiers) =>
            new KeepHighestMode(amount, rollModifiers);

        public static IMode KeepLowest(int amount, params IRollModifier[] rollModifiers) =>
            new KeepLowestMode(amount, rollModifiers);

        public static IMode DropHighest(int amount, params IRollModifier[] rollModifiers) =>
            new DropHighestMode(amount, rollModifiers);

        public static IMode DropLowest(int amount, params IRollModifier[] rollModifiers) =>
            new DropLowestMode(amount, rollModifiers);
    }

    public interface IMode
    {
        public IRollModifier[] RollModifiers { get; init; }

        public DiceResult Evaluate(int amount, DiceRange diceRange, IDiceRollHandlers handler);
    }

    private record DefaultMode(IRollModifier[] RollModifiers) : IMode
    {
        public DiceResult Evaluate(int amount, DiceRange diceRange, IDiceRollHandlers handler)
        {
            DiceResultInt[] rolls = Enumerable.Range(1, amount)
                .Select(_ => new DiceRoll(diceRange, RollModifiers).Roll(handler))
                .OrderByDescending(dr => dr.Value)
                .ToArray();

            int total = rolls.Select(r => r.Value).Sum();
            return new DiceResult(total, string.Join(", ", rolls.Select(r => r.Expression)));
        }
    }

    private record KeepHighestMode(int Amount, IRollModifier[] RollModifiers) : IMode
    {
        public DiceResult Evaluate(int amount, DiceRange diceRange, IDiceRollHandlers handler)
        {
            DiceResultInt[] rolls = Enumerable.Range(0, amount)
                .Select(_ => new DiceRoll(diceRange, RollModifiers).Roll(handler))
                .OrderByDescending(n => n.Value)
                .ToArray();

            int total = rolls
                .Take(Amount)
                .Select(r => r.Value)
                .Sum();

            return new DiceResult(total,
                $"{string.Join(", ", rolls.Take(Amount).Select(r => r.Expression))}, " +
                $"{string.Join(", ", rolls.Skip(Amount).Select(r => r.Expression + "d"))}");
        }
    }

    private record KeepLowestMode(int Amount, IRollModifier[] RollModifiers) : IMode
    {
        public DiceResult Evaluate(int amount, DiceRange diceRange, IDiceRollHandlers handler)
        {
            DiceResultInt[] rolls = Enumerable.Range(0, amount)
                .Select(_ => new DiceRoll(diceRange, RollModifiers).Roll(handler))
                .OrderBy(n => n)
                .ToArray();

            int total = rolls
                .Take(Amount)
                .Select(r => r.Value)
                .Sum();

            return new DiceResult(total,
                $"{string.Join(", ", rolls.Take(Amount).Select(r => r.Expression))}, " +
                $"{string.Join(", ", rolls.Skip(Amount).Select(r => r.Expression + "d"))}");
        }
    }

    private record DropHighestMode(int Amount, IRollModifier[] RollModifiers) : IMode
    {
        public DiceResult Evaluate(int amount, DiceRange diceRange, IDiceRollHandlers handler)
        {
            DiceResultInt[] rolls = Enumerable.Range(0, amount)
                .Select(_ => new DiceRoll(diceRange, RollModifiers).Roll(handler))
                .OrderByDescending(n => n)
                .ToArray();

            int total = rolls
                .Skip(Amount)
                .Select(r => r.Value)
                .Sum();

            return new DiceResult(total,
                $"{string.Join(", ", rolls.Reverse().Skip(Amount).Select(r => r.Expression))}, " +
                $"{string.Join(", ", rolls.Reverse().Take(Amount).Select(r => r.Expression + "d"))}");
        }
    }

    private record DropLowestMode(int Amount, IRollModifier[] RollModifiers) : IMode
    {
        public DiceResult Evaluate(int amount, DiceRange diceRange, IDiceRollHandlers handler)
        {
            DiceResultInt[] rolls = Enumerable.Range(0, amount)
                .Select(_ => new DiceRoll(diceRange, RollModifiers).Roll(handler))
                .OrderBy(n => n)
                .ToArray();

            int total = rolls
                .Skip(Amount)
                .Select(r => r.Value)
                .Sum();

            return new DiceResult(total,
                $"{string.Join(", ", rolls.Reverse().Skip(Amount).Select(r => r.Expression))}, " +
                $"{string.Join(", ", rolls.Reverse().Take(Amount).Select(r => r.Expression + "d"))}");
        }
    }
}

public record NumberExpression(int Number) : IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler) => new(Number, Number.ToString());
}

public record BinaryExpression(IExpression Left, char Operator, IExpression Right) : IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler)
    {
        DiceResult leftResult = Left.Evaluate(handler);
        DiceResult rightResult = Right.Evaluate(handler);

        float result = Operator switch
        {
            Tokens.ADD => leftResult.Value + rightResult.Value,
            Tokens.SUB => leftResult.Value - rightResult.Value,
            Tokens.MUL => leftResult.Value * rightResult.Value,
            Tokens.DIV => leftResult.Value / rightResult.Value,
            _ => throw new Exception($"Unexpected operator: {Operator}")
        };

        return new DiceResult(result, $"{leftResult.Expression} {Operator} {rightResult.Expression}");
    }
}

public record ParenExpression(IExpression Expression) : IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler)
    {
        DiceResult diceResult = Expression.Evaluate(handler);
        return diceResult with { Expression = $"({diceResult.Expression})" };
    }
}