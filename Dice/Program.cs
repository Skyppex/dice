using System.Diagnostics;
using Dice;

Thread.Sleep(10000);
Args parsedArgs = new ArgsParser(args).ParseArgs();

Stopwatch? stopwatch = null;

if (parsedArgs == Args.Empty)
{
    Help.Print();
    return;
}

if (parsedArgs.Timer)
    stopwatch = Stopwatch.StartNew();

try
{
    DiceResult diceResult = parsedArgs.Mode.Evaluate(parsedArgs.Roll);
    PrintResults(
        diceResult.Value,
        parsedArgs.PrintExpression ? diceResult.Expression : string.Empty,
        stopwatch
    );
}
catch (NotSupportedException e)
{
    Console.WriteLine($"Error: {e.Message}");
}

return;

static void PrintResults(
    float result,
    string expressionString,
    Stopwatch? stopwatch
)
{
    if (!string.IsNullOrEmpty(expressionString))
        Console.WriteLine($"Expression: {expressionString}");

    Console.WriteLine($"Result: {result}");

    if (stopwatch != null)
        Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
}
