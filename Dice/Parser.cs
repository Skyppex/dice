﻿namespace Dice;

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

        if (!_tokens.TryPeek(out IToken nextToken) || nextToken is not OperatorToken operatorToken)
            return left;
        
        if (operatorToken.Operator is not (Tokens.ADD or Tokens.SUB))
            return left;
        
        _tokens.Dequeue();
        IExpression right = ParseMultiplicative();
        return new BinaryExpression(left, operatorToken.Operator, right);

    }

    private IExpression ParseMultiplicative()
    {
        IExpression left = ParsePrimary();
        
        if (!_tokens.TryPeek(out IToken nextToken) || nextToken is not OperatorToken operatorToken)
            return left;
        
        if (operatorToken.Operator is not (Tokens.MUL or Tokens.DIV or Tokens.MOD))
            return left;
        
        _tokens.Dequeue();
        IExpression right = ParsePrimary();
        return new BinaryExpression(left, operatorToken.Operator, right);
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
                    _tokens.Dequeue();
                    
                    var secondNumberToken = Expect<NumberToken>("Expected number after d");
                    return new DiceExpression(numberToken.Number, secondNumberToken.Number);
                }
                
                return new NumberExpression(numberToken.Number);
            }
            
        }
        
        throw new Exception($"Unexpected token: {token.Value}");
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

public record DiceExpression(int Amount, int Dice) : IExpression
{
    public string Symbol => $"{Amount}d{Dice}";
    
    public DiceResult Evaluate()
    {
        var rand = Random.Shared;
        int total = 0;
        
        for (int i = 0; i < Amount; i++)
            total += rand.Next(1, Dice + 1);

        return new DiceResult(total, $"'{total}'");
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