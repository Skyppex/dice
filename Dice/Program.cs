using Dice;

string input = string.Join(" ", args);
Console.WriteLine($"Input: {input}");

Queue<IToken> tokens = new Tokenizer().Tokenize(input);

IExpression expression = Parser.Parse(tokens);

DiceResult diceResult = expression.Evaluate();
string output = diceResult.Value + $" | {diceResult.Expression}";

Console.WriteLine($"Output: {output}");