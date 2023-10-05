﻿using System.Diagnostics;
using Dice;

Args parsedArgs = new ArgsParser(args).ParseArgs();

Stopwatch? stopwatch = null;

if (parsedArgs.SpeedTimer)
    stopwatch = Stopwatch.StartNew();

if (parsedArgs == Args.Empty)
{
    Help.Print();
    return;
}

DiceResult diceResult = parsedArgs.Mode.Evaluate(parsedArgs.Roll);
PrintResults(diceResult.Value, parsedArgs.PrintExpression ? diceResult.Expression : string.Empty, stopwatch);

return;

static void PrintResults(float result, string expressionString, Stopwatch? stopwatch)
{
    if (!string.IsNullOrEmpty(expressionString))
        Console.WriteLine($"Expression: {expressionString}");
        
    Console.WriteLine($"Result: {result}");

    if (stopwatch != null)
        Console.WriteLine($"\nSpeed: {stopwatch.ElapsedMilliseconds}ms");
}
