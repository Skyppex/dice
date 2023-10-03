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
string output = diceResult.Value + $" | {diceResult.Expression}";

Console.WriteLine($"Output: {output}");