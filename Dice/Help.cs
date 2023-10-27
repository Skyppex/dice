using System.Text;

namespace Dice;

public static class Help
{
    public static void Print()
    {
        StringBuilder builder = new();
        builder.AppendLine("Usage: dice [options] <expression>");
        builder.AppendLine();
        builder.AppendLine("Options:");
        builder.AppendLine(WriteExplanation("-h, -?, --help", "Print this help message"));
        builder.AppendLine(WriteExplanation("-e, --expression", "Print the expression used to calculate the result"));
        builder.AppendLine(WriteExplanation("-t, --time", "Print the time it took to calculate the result"));
        builder.AppendLine(WriteExplanation("-m, --mode", "Specify the evaluation mode"));
        builder.AppendLine(WriteExplanation("    simavg:<iterations>", "Evaluate the expression <iterations> times and average the results (simulated average)"));
        builder.AppendLine(WriteExplanation("    avg, average", "Evaluate the expression once with the average of each roll"));
        builder.AppendLine(WriteExplanation("    max", "Evaluate the expression once with the maximum possible rolls"));
        builder.AppendLine(WriteExplanation("    min", "Evaluate the expression once with the minimum possible rolls"));
        builder.AppendLine(WriteExplanation("    med, median", "Evaluate the expression once with the median of each roll"));
        builder.AppendLine();
        builder.AppendLine("Make a dice roll:");
        builder.AppendLine(WriteExplanation("<rolls>d<sides>", "Roll <rolls> dice with <sides> sides and sum them together -> 4d6"));
        builder.AppendLine(WriteExplanation("<rolls>d[<min>,<max>]", "Roll <rolls> dice with <min> to <max> sides and sum them together -> 4d[1,6]"));
        builder.AppendLine(WriteExplanation(@"<rolls>d[<value>\]*", @"Roll <rolls> dice with <values> as faces and sum them together -> 4d[1\3\5]"));
        builder.AppendLine();
        builder.AppendLine("Modifiers:");
        builder.AppendLine(WriteExplanation("k(<keep>)", "Keep the highest <keep> (default 1) -> 4d6k3"));
        builder.AppendLine(WriteExplanation("kl(<keep>)", "Keep the lowest <keep> (default 1) -> 4d6kl3"));
        builder.AppendLine(WriteExplanation("dh(<drop>)", "Drop the highest <drop> (default 1) -> 4d6dh3"));
        builder.AppendLine(WriteExplanation("dl(<drop>)", "Drop the highest <drop> (default 1) -> 4d6dl3"));
        builder.AppendLine(WriteExplanation("!(<explode>)", "Explode dice up to <explode> number of times (default 1) -> 4d6!3"));
        builder.AppendLine(WriteExplanation("!!(<explode>)", "Explode dice up to <explode> number of times (default 1) and sum the results -> 4d6!!3"));
        builder.AppendLine(WriteExplanation("r(<max>)", "Re-roll dice up to <max> number of times (default 100) -> 4d6r3"));
        builder.AppendLine(WriteExplanation("u(<max>)", "If the result is not unique, re-rolls dice up to <max> number of times (default 100) -> 4d6u3"));
        builder.AppendLine();
        builder.AppendLine("Conditionals:");
        builder.AppendLine(WriteExplanation("[<operator>][<value>]", "Check if the roll is <operator> <value> (Success = 1, Failure = 0)-> 4d6>3"));
        builder.AppendLine(WriteExplanation("    =", "Equal to"));
        builder.AppendLine(WriteExplanation("    =!", "Not equal to (cannot use '!=' due to a collision with 'explode')"));
        builder.AppendLine(WriteExplanation("    >", "Greater than"));
        builder.AppendLine(WriteExplanation("    <", "Less than"));
        builder.AppendLine(WriteExplanation("    >=", "Greater than or equal to"));
        builder.AppendLine(WriteExplanation("    <=", "Less than or equal to"));
        builder.AppendLine(WriteExplanation("    >[<value1>]<[<value2>]", "Greater than <value1> and less than <value2>"));
        builder.AppendLine(WriteExplanation("", "Conditionals can be used to determine whether or not to re-roll a die -> 4d6r<=4"));
        builder.AppendLine();
        builder.AppendLine("Basic math operators:");
        builder.AppendLine(WriteExplanation("+", "Addition -> 1d6 + 1d6 + 2"));
        builder.AppendLine(WriteExplanation("-", "Subtraction -> 1d6 - 1d6 + 2"));
        builder.AppendLine(WriteExplanation("*", "Multiplication -> 1d6 * 1d6 + 2"));
        builder.AppendLine(WriteExplanation("/", "division -> 1d6 / 1d6 + 2"));
        builder.AppendLine(WriteExplanation("(<expression>)", "Parentheses -> 1d6 * (1d6 + 2)"));
        
        Console.Write(builder.ToString());
    }

    private static string WriteExplanation(string expression, string explanation)
    {
        int expressionLength = expression.Length;
        const int PADDING = 30;
        int padding = PADDING - expressionLength;
        return $"    {expression}{new string(' ', padding)}{explanation}";
    }
}