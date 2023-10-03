using Dice;

var input = string.Join(" ", args);
Console.WriteLine($"Input: {input}");

var tokens = new Tokenizer().Tokenize(input);

var expression = Parser.Parse(tokens);

var diceResult = expression.Evaluate();
var output = diceResult.Result + $" ({diceResult.Expression})";

Console.WriteLine($"Output: {output}");