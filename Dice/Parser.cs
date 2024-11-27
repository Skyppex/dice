using System.Diagnostics;

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
            Console.WriteLine(
                $"Found unexpected tokens at the end of the expression: {string.Join("", parser._tokens.Select(t => t.ToString()))}\n"
                    + $"Will evaluate excluding the unexpected tokens."
            );

        return expression;
    }

    private IExpression ParseExpression() => ParseAdditive();

    private IExpression ParseAdditive()
    {
        IExpression left = ParseMultiplicative();

        while (
            _tokens.TryPeek(out IToken? nextToken)
            && nextToken is OperatorToken operatorToken
        )
        {
            if (operatorToken.Operator is not (Tokens.ADD or Tokens.SUB))
                break;

            _tokens.Dequeue();
            IExpression right = ParseMultiplicative();
            left = new BinaryExpression(left, operatorToken.Operator, right);
        }

        return left;
    }

    private IExpression ParseMultiplicative()
    {
        IExpression left = ParseUnary();

        while (
            _tokens.TryPeek(out IToken? nextToken)
            && nextToken is OperatorToken operatorToken
        )
        {
            if (
                operatorToken.Operator
                is not (Tokens.MUL or Tokens.DIV or Tokens.MOD)
            )
                break;

            _tokens.Dequeue();
            IExpression right = ParseUnary();
            left = new BinaryExpression(left, operatorToken.Operator, right);
        }

        return left;
    }

    private IExpression ParseUnary()
    {
        while (
            _tokens.TryPeek(out IToken? nextToken)
            && nextToken is OperatorToken operatorToken
        )
        {
            if (operatorToken.Operator is not (Tokens.ADD or Tokens.SUB))
                break;

            _tokens.Dequeue();
            IExpression right = ParsePrimary();
            return new UnaryExpression(operatorToken.Operator, right);
        }

        return ParsePrimary();
    }

    private IExpression ParsePrimary()
    {
        if (!_tokens.TryDequeue(out IToken? token))
            throw new Exception("Unexpected end of input");

        switch (token)
        {
            case NumberToken numberToken:
            {
                if (
                    _tokens.TryPeek(out IToken? nextToken)
                    && nextToken is DiceToken
                )
                    return ParseDiceRoll(numberToken);

                return new NumberExpression(numberToken.Number);
            }

            case OpenParenToken:
            {
                IExpression expression = ParseExpression();
                Expect<CloseParenToken>("Expected )");
                return new ParenExpression(expression);
            }
        }

        throw new Exception($"Unexpected token: {token}");
    }

    private IExpression ParseDiceRoll(NumberToken numberToken)
    {
        _tokens.Dequeue();

        var dice = ParseDice();

        List<IRollModifier> rollModifiers = ParseRollModifiers();

        if (!_tokens.TryPeek(out IToken? nextToken))
            return new DiceExpression(
                numberToken.Number,
                dice,
                DiceExpression.Modes.Default(rollModifiers.ToArray())
            );

        switch (nextToken)
        {
            case KeepToken:
            {
                _tokens.Dequeue();
                return ParseKeep(numberToken, dice, rollModifiers);
            }

            case DropToken:
            {
                _tokens.Dequeue();

                if (
                    ParseDrop(
                        numberToken,
                        dice,
                        rollModifiers,
                        out IExpression? dropExpression
                    )
                )
                    return dropExpression!;

                break;
            }
        }

        return new DiceExpression(
            numberToken.Number,
            dice,
            DiceExpression.Modes.Default(rollModifiers.ToArray())
        );
    }

    private IDice ParseDice()
    {
        if (!_tokens.TryPeek(out IToken? nextToken))
            throw new Exception("Unexpected end of tokens");

        switch (nextToken)
        {
            case NumberToken numberToken:
            {
                _tokens.Dequeue();
                return new DiceRange(
                    1,
                    numberToken.Number,
                    1,
                    IDice.DefaultFormat
                );
            }

            case FudgeFateToken:
            {
                _tokens.Dequeue();
                return new DiceRange(
                    -1,
                    1,
                    1,
                    v =>
                        v switch
                        {
                            < 0 => "-",
                            0 => "0",
                            > 0 => "+",
                            _ => throw new UnreachableException(
                                $"Unexpected value: {v}"
                            ),
                        }
                );
            }

            case OpenBracketToken:
            {
                _tokens.Dequeue();
                var minToken = Expect<NumberToken>("Expected number");

                var (delimiterToken, _) = ExpectEither<DelimiterToken, OrToken>(
                    "Expected , or |"
                );

                if (delimiterToken is not null)
                {
                    var maxToken = Expect<NumberToken>("Expected number");
                    Expect<CloseBracketToken>("Expected ]");
                    return new DiceRange(
                        minToken.Number,
                        maxToken.Number,
                        1,
                        IDice.DefaultFormat
                    );
                }

                List<int> values = new() { minToken.Number };
                values.Add(Expect<NumberToken>("Expected number").Number);

                while (
                    _tokens.TryPeek(out IToken? t) && t is not CloseBracketToken
                )
                {
                    Expect<OrToken>("Expected |");
                    values.Add(Expect<NumberToken>("Expected number").Number);
                }

                Expect<CloseBracketToken>("Expected ]");

                return new DiceValues(values, 1, IDice.DefaultFormat);
            }
        }

        throw new Exception($"Unexpected token: {nextToken}");
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

                    if (_tokens.TryPeek(out IToken? t) && t is ExplodeToken)
                    {
                        _tokens.Dequeue();
                        ExpectNumberInfiniteOrDefault(
                            rollModifiers,
                            n => new ExplodeModifier(n, Combined: true),
                            () => new ExplodeModifier(Combined: true)
                        );
                        break;
                    }

                    ExpectNumberInfiniteOrDefault(
                        rollModifiers,
                        n => new ExplodeModifier(n),
                        () => new ExplodeModifier()
                    );
                    break;
                }

                case ReRollToken:
                {
                    _tokens.Dequeue();

                    if (_tokens.TryPeek(out IToken? secondNextToken))
                    {
                        int? maxReRolls = null;

                        if (secondNextToken is NumberToken numberOfReRollsToken)
                        {
                            maxReRolls = numberOfReRollsToken.Number;
                            _tokens.Dequeue();

                            if (!_tokens.TryPeek(out secondNextToken))
                                rollModifiers.Add(
                                    new ReRollModifier(
                                        rollModifiers.ToArray(),
                                        maxReRolls.Value
                                    )
                                );
                        }
                        else if (secondNextToken is InfiniteToken)
                        {
                            maxReRolls = int.MaxValue;
                            _tokens.Dequeue();

                            if (!_tokens.TryPeek(out secondNextToken))
                                rollModifiers.Add(
                                    new ReRollModifier(
                                        rollModifiers.ToArray(),
                                        maxReRolls.Value
                                    )
                                );
                        }

                        if (secondNextToken is ConditionToken conditionToken)
                        {
                            _tokens.Dequeue();
                            var numberToken = Expect<NumberToken>(
                                "Expected number"
                            );
                            var condition = conditionToken.GetCondition(
                                numberToken.Number
                            );
                            rollModifiers.Add(
                                new ReRollModifier(
                                    rollModifiers.ToArray(),
                                    maxReRolls ?? 1,
                                    condition
                                )
                            );
                            break;
                        }

                        rollModifiers.Add(
                            new ReRollModifier(
                                rollModifiers.ToArray(),
                                maxReRolls ?? 1
                            )
                        );
                        break;
                    }

                    rollModifiers.Add(
                        new ReRollModifier(rollModifiers.ToArray())
                    );
                    break;
                }

                case UniqueToken:
                {
                    _tokens.Dequeue();
                    ExpectNumberInfiniteOrDefault(
                        rollModifiers,
                        n => new UniqueModifier(rollModifiers.ToArray(), n),
                        () => new UniqueModifier(rollModifiers.ToArray())
                    );
                    break;
                }

                case ConditionToken:
                {
                    List<ConditionModifier.Condition> conditions = new();

                    do
                    {
                        var conditionToken = (ConditionToken)_tokens.Dequeue();
                        var numberToken = Expect<NumberToken>(
                            "Expected number"
                        );
                        conditions.Add(
                            new ConditionModifier.Condition(
                                conditionToken.ConditionalOperator,
                                numberToken.Number
                            )
                        );
                    } while (
                        _tokens.TryPeek(out IToken? token)
                        && token is ConditionToken
                    );

                    rollModifiers.Add(new ConditionModifier(conditions));

                    break;
                }

                default:
                    return rollModifiers;
            }
        }

        return rollModifiers;
    }

    private void ExpectNumberInfiniteOrDefault(
        List<IRollModifier> rollModifiers,
        Func<int, IRollModifier> modifierGetter,
        Func<IRollModifier> defaultModifierGetter
    )
    {
        if (_tokens.TryPeek(out IToken? secondNextToken))
        {
            if (secondNextToken is NumberToken numberToken)
            {
                _tokens.Dequeue();
                rollModifiers.Add(modifierGetter(numberToken.Number));
                return;
            }

            if (secondNextToken is InfiniteToken)
            {
                _tokens.Dequeue();
                rollModifiers.Add(modifierGetter(int.MaxValue));
                return;
            }
        }

        rollModifiers.Add(defaultModifierGetter());
    }

    private IExpression ParseKeep(
        NumberToken numberToken,
        IDice dice,
        List<IRollModifier> rollModifiers
    )
    {
        if (_tokens.TryPeek(out IToken? nextNextToken))
        {
            switch (nextNextToken)
            {
                case LowestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    return new DiceExpression(
                        numberToken.Number,
                        dice,
                        DiceExpression.Modes.KeepLowest(
                            amount.Number,
                            rollModifiers.ToArray()
                        )
                    );
                }

                case HighestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    return new DiceExpression(
                        numberToken.Number,
                        dice,
                        DiceExpression.Modes.KeepHighest(
                            amount.Number,
                            rollModifiers.ToArray()
                        )
                    );
                }

                default:
                {
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    return new DiceExpression(
                        numberToken.Number,
                        dice,
                        DiceExpression.Modes.KeepHighest(
                            amount.Number,
                            rollModifiers.ToArray()
                        )
                    );
                }
            }
        }

        return new DiceExpression(
            numberToken.Number,
            dice,
            DiceExpression.Modes.KeepHighest(1, rollModifiers.ToArray())
        );
    }

    private bool ParseDrop(
        NumberToken numberToken,
        IDice dice,
        List<IRollModifier> rollModifiers,
        out IExpression? dropExpression
    )
    {
        if (_tokens.TryPeek(out IToken? nextNextToken))
        {
            switch (nextNextToken)
            {
                case HighestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    dropExpression = new DiceExpression(
                        numberToken.Number,
                        dice,
                        DiceExpression.Modes.DropHighest(
                            amount.Number,
                            rollModifiers.ToArray()
                        )
                    );

                    return true;
                }

                case LowestToken:
                {
                    _tokens.Dequeue();
                    NumberToken amount = ExpectOrElse(() => new NumberToken(1));

                    dropExpression = new DiceExpression(
                        numberToken.Number,
                        dice,
                        DiceExpression.Modes.DropLowest(
                            amount.Number,
                            rollModifiers.ToArray()
                        )
                    );

                    return true;
                }

                default:
                    throw new Exception(
                        $"Unexpected token: {nextNextToken}. Expected h or l after d when choosing to drop dice"
                    );
            }
        }

        dropExpression = null;
        return false;
    }

    private TToken Expect<TToken>(string errorMessage)
        where TToken : IToken
    {
        if (!_tokens.TryPeek(out IToken? nextToken) || nextToken is not TToken)
            throw new Exception(errorMessage);

        return (TToken)_tokens.Dequeue();
    }

    private (TToken1?, TToken2?) ExpectEither<TToken1, TToken2>(
        string errorMessage
    )
        where TToken1 : IToken
        where TToken2 : IToken
    {
        if (
            !_tokens.TryPeek(out IToken? nextToken)
            || nextToken is not TToken1 and not TToken2
        )
            throw new Exception(errorMessage);

        var token = _tokens.Dequeue();

        if (token is TToken1 token1)
            return (token1, default);

        if (token is TToken2 token2)
            return (default, token2);

        throw new UnreachableException($"Unexpected token: {token}");
    }

    private TToken ExpectOrDefault<TToken>(TToken defaultValue)
        where TToken : IToken
    {
        if (!_tokens.TryPeek(out IToken? nextToken) || nextToken is not TToken)
            return defaultValue;

        return (TToken)_tokens.Dequeue();
    }

    private TToken ExpectOrElse<TToken>(Func<TToken> defaultGetter)
        where TToken : IToken
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

public record DiceExpression(
    int Amount,
    IDice DiceRange,
    DiceExpression.IMode Mode
) : IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler)
    {
        DiceResult diceResult = Mode.Evaluate(Amount, DiceRange, handler);
        return diceResult with { Expression = $"[{diceResult.Expression}]" };
    }

    public static class Modes
    {
        public static IMode Default(params IRollModifier[] rollModifiers) =>
            new DefaultMode(rollModifiers);

        public static IMode KeepHighest(
            int amount,
            params IRollModifier[] rollModifiers
        ) => new KeepHighestMode(amount, rollModifiers);

        public static IMode KeepLowest(
            int amount,
            params IRollModifier[] rollModifiers
        ) => new KeepLowestMode(amount, rollModifiers);

        public static IMode DropHighest(
            int amount,
            params IRollModifier[] rollModifiers
        ) => new DropHighestMode(amount, rollModifiers);

        public static IMode DropLowest(
            int amount,
            params IRollModifier[] rollModifiers
        ) => new DropLowestMode(amount, rollModifiers);
    }

    public interface IMode
    {
        public DiceResult Evaluate(
            int amount,
            IDice dice,
            IDiceRollHandlers handler
        );
    }

    private record DefaultMode(IRollModifier[] RollModifiers) : IMode
    {
        public DiceResult Evaluate(
            int amount,
            IDice dice,
            IDiceRollHandlers handler
        )
        {
            List<float> runningRolls = new();
            DiceResult[] rolls = Enumerable
                .Range(1, amount)
                .Select(_ =>
                {
                    var result = new DiceRoll(dice, RollModifiers).Roll(
                        handler,
                        runningRolls
                    );
                    runningRolls.Add(result.Value);
                    return result;
                })
                .OrderByDescending(dr => dr.Value)
                .ToArray();

            float total = rolls.Select(r => r.Value).Sum();
            return new DiceResult(
                total,
                string.Join(", ", rolls.Select(r => r.Expression))
            );
        }
    }

    private record KeepHighestMode(int Amount, IRollModifier[] RollModifiers)
        : IMode
    {
        public DiceResult Evaluate(
            int amount,
            IDice dice,
            IDiceRollHandlers handler
        )
        {
            List<float> runningRolls = new();
            DiceResult[] rolls = Enumerable
                .Range(0, amount)
                .Select(_ =>
                {
                    var result = new DiceRoll(dice, RollModifiers).Roll(
                        handler,
                        runningRolls
                    );
                    runningRolls.Add(result.Value);
                    return result;
                })
                .OrderByDescending(n => n.Value)
                .ToArray();

            float total = rolls.Take(Amount).Select(r => r.Value).Sum();

            return new DiceResult(
                total,
                $"{string.Join(", ", rolls.Take(Amount).Select(r => r.Expression))}, "
                    + $"{string.Join(", ", rolls.Skip(Amount).Select(r => r.Expression + "d"))}"
            );
        }
    }

    private record KeepLowestMode(int Amount, IRollModifier[] RollModifiers)
        : IMode
    {
        public DiceResult Evaluate(
            int amount,
            IDice dice,
            IDiceRollHandlers handler
        )
        {
            List<float> runningRolls = new();
            DiceResult[] rolls = Enumerable
                .Range(0, amount)
                .Select(_ =>
                {
                    var result = new DiceRoll(dice, RollModifiers).Roll(
                        handler,
                        runningRolls
                    );
                    runningRolls.Add(result.Value);
                    return result;
                })
                .OrderBy(n => n.Value)
                .ToArray();

            float total = rolls.Take(Amount).Select(r => r.Value).Sum();

            return new DiceResult(
                total,
                $"{string.Join(", ", rolls.Take(Amount).Select(r => r.Expression))}, "
                    + $"{string.Join(", ", rolls.Skip(Amount).Select(r => r.Expression + "d"))}"
            );
        }
    }

    private record DropHighestMode(int Amount, IRollModifier[] RollModifiers)
        : IMode
    {
        public DiceResult Evaluate(
            int amount,
            IDice dice,
            IDiceRollHandlers handler
        )
        {
            List<float> runningRolls = new();
            DiceResult[] rolls = Enumerable
                .Range(0, amount)
                .Select(_ =>
                {
                    var result = new DiceRoll(dice, RollModifiers).Roll(
                        handler,
                        runningRolls
                    );
                    runningRolls.Add(result.Value);
                    return result;
                })
                .OrderByDescending(n => n.Value)
                .ToArray();

            float total = rolls.Skip(Amount).Select(r => r.Value).Sum();

            return new DiceResult(
                total,
                $"{string.Join(", ", rolls.Reverse().Skip(Amount).Select(r => r.Expression))}, "
                    + $"{string.Join(", ", rolls.Reverse().Take(Amount).Select(r => r.Expression + "d"))}"
            );
        }
    }

    private record DropLowestMode(int Amount, IRollModifier[] RollModifiers)
        : IMode
    {
        public DiceResult Evaluate(
            int amount,
            IDice dice,
            IDiceRollHandlers handler
        )
        {
            List<float> runningRolls = new();
            DiceResult[] rolls = Enumerable
                .Range(0, amount)
                .Select(_ =>
                {
                    var result = new DiceRoll(dice, RollModifiers).Roll(
                        handler,
                        runningRolls
                    );
                    runningRolls.Add(result.Value);
                    return result;
                })
                .OrderBy(n => n.Value)
                .ToArray();

            float total = rolls.Skip(Amount).Select(r => r.Value).Sum();

            return new DiceResult(
                total,
                $"{string.Join(", ", rolls.Reverse().Skip(Amount).Select(r => r.Expression))}, "
                    + $"{string.Join(", ", rolls.Reverse().Take(Amount).Select(r => r.Expression + "d"))}"
            );
        }
    }
}

public record NumberExpression(int Number) : IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler) =>
        new(Number, Number.ToString());
}

public record BinaryExpression(
    IExpression Left,
    char Operator,
    IExpression Right
) : IExpression
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
            Tokens.MOD => leftResult.Value % rightResult.Value,
            _ => throw new Exception($"Unexpected operator: {Operator}"),
        };

        return new DiceResult(
            result,
            $"{leftResult.Expression} {Operator} {rightResult.Expression}"
        );
    }
}

public record UnaryExpression(char Operator, IExpression Expression)
    : IExpression
{
    public DiceResult Evaluate(IDiceRollHandlers handler)
    {
        DiceResult rightResult = Expression.Evaluate(handler);

        float result = Operator switch
        {
            Tokens.ADD => +rightResult.Value,
            Tokens.SUB => -rightResult.Value,
            _ => throw new Exception($"Unexpected operator: {Operator}"),
        };

        return new DiceResult(result, $"{Operator}{rightResult.Expression}");
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
