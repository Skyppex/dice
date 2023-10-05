using Dice;

string dice = "";

#if DEBUG
Console.WriteLine($"Input: {dice}");
#endif

Args parsedArgs = new ArgsParser(args).ParseArgs();

if (parsedArgs == Args.Empty)
{
    Help.Print();
    return;
}

DiceResult diceResult = parsedArgs.Mode.Evaluate(parsedArgs.Roll);
PrintResults(diceResult.Value, parsedArgs.PrintExpression ? diceResult.Expression : string.Empty);

return;

static void PrintResults(float result, string expressionString)
{
    if (!string.IsNullOrEmpty(expressionString))
        Console.WriteLine($"Expression: {expressionString}");
        
    Console.WriteLine($"Result: {result}");
}
