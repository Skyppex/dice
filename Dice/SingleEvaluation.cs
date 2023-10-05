namespace Dice;

public interface IEvaluationMode
{
    public DiceResult Evaluate(string roll);
}

public record NoEvaluation : IEvaluationMode
{
    public DiceResult Evaluate(string roll) => throw new NotSupportedException();
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
        
         // float total = 0;
         List<DiceResult> diceResults = new();

         Parallel.For(0, Iterations, _ =>
         {
            DiceResult diceResult = expression.Evaluate(new RandomRollHandler(Random.Shared));
            diceResults.Add(diceResult);
         });
         
         float total
         // for (int i = 0; i < Iterations; i++)
         // {
         //     DiceResult diceResult = expression.Evaluate(new RandomRollHandler(Random.Shared));
         //     total += diceResult.Value;
         // }
         
         return new DiceResult(total / Iterations, $"Rolled ({roll}) {Iterations} times and took the average.");
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