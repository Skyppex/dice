using System.Text;

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
        return expression.Evaluate(new RandomRollHandler());
    }
}

public record CalculatedAverageEvaluation : IEvaluationMode
{
    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);
        return expression.Evaluate(new AverageRollHandler());
    }
}

public record SimulatedAverageEvaluation(int Iterations) : IEvaluationMode
{
    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);

        float average = Enumerable
            .Range(0, Iterations)
            .AsParallel()
            .Select(_ => expression.Evaluate(new RandomRollHandler()))
            .Average(dr => dr.Value);

        return new DiceResult(average, $"Rolled ({roll}) {Iterations} times and took the average.");
    }
}

public record SimulatedGraphEvaluation(int Iterations) : IEvaluationMode
{
    private const string BLOCK = "\u2588";

    public DiceResult Evaluate(string roll)
    {
        Queue<IToken> tokens = new Tokenizer().Tokenize(roll);
        IExpression expression = Parser.Parse(tokens);

        var rolls = Enumerable
            .Range(0, Iterations)
            .AsParallel()
            .Select(_ => expression.Evaluate(new RandomRollHandler()).Value)
            .ToList();

        float average = rolls.Average();

        var groups = rolls.GroupBy(r => r).OrderBy(g => g.Key);
        var distribution = groups.Select(g => g.Count()).ToList();
        int distributionCount = distribution.Count;

        int min = distribution.Min();
        int max = distribution.Max();

        const int HEIGHT = 7;

        var graph = new StringBuilder();

        for (int y = HEIGHT + 1; y >= 1; y--)
        {
            for (int x = 0; x < distributionCount; x++)
            {
                float count = (((float)distribution[x] - min) / (max - min)) * HEIGHT;
                graph.Append(count >= y ? BLOCK : " ");
            }
            graph.AppendLine();
        }

        return new DiceResult(average, $"Distribution of ({roll}) {Iterations} times:\n{graph}");
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
