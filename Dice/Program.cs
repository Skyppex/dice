using Dice;

string input = string.Join(" ", args);

#if DEBUG
Console.WriteLine($"Input: {input}");
#endif

if (input is "" or "h" or "-h" or "--h" or "help" or "-help" or "--help" or "-?" or "--?" or "/?")
{
    Help.Print();
    return;
}

Queue<IToken> tokens = new Tokenizer().Tokenize(input);

IExpression expression = Parser.Parse(tokens);

DiceResult diceResult = expression.Evaluate();
float output = diceResult.Value;
string expressionString = diceResult.Expression;

Console.WriteLine($"Expression: {expressionString}");
Console.WriteLine($"Result: {output}");
