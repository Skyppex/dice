using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dice;

public interface IEvaluationMode
{
    public DiceResult Evaluate(string roll);
}

public record NoEvaluation : IEvaluationMode
{
    public DiceResult Evaluate(string roll) =>
        throw new NotSupportedException("Cannot evaluate with no evaluation mode.");
}

public record SingleEvaluation : IEvaluationMode
{
    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);
        return expression.Evaluate(new RandomRollHandler(Random.Shared));
    }
}

public record SimulatedAverageEvaluation(int Iterations) : IEvaluationMode
{
    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);

        float average = Enumerable.Range(0, Iterations)
            .AsParallel()
            .Select(_ => expression.Evaluate(new RandomRollHandler(Random.Shared)))
            .Average(dr => dr.Value);
        
        return new DiceResult(average, $"Rolled ({roll}) {Iterations} times and took the average.");
    }
}

public record MaximumEvaluation : IEvaluationMode
{
    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);
        
        return expression.Evaluate(new MaxRollHandler());
    }
}

public record MinimumEvaluation : IEvaluationMode
{
    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);
        
        return expression.Evaluate(new MinRollHandler());
    }
}

public record MedianEvaluation : IEvaluationMode
{
    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);
        
        return expression.Evaluate(new MedianRollHandler());
    }
}