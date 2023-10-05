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
        builder.AppendLine(WriteExplanation("-h, -help, -?", "Print this help message"));
        builder.AppendLine(WriteExplanation("-e, -expression", "Print the expression used to calculate the result"));
        builder.AppendLine(WriteExplanation("-m, -mode", "Specify the evaluation mode"));
        builder.AppendLine(WriteExplanation("-t, -time", "Print the time it took to calculate the result"));
        builder.AppendLine(WriteExplanation("    simavg:<iterations>", "Evaluate the expression <iterations> times and average the results (simulated average)"));
        builder.AppendLine(WriteExplanation("    avg, average", "Evaluate the expression once with the average of each roll"));
        builder.AppendLine(WriteExplanation("    max", "Evaluate the expression once with the maximum possible rolls"));
        builder.AppendLine(WriteExplanation("    min", "Evaluate the expression once with the minimum possible rolls"));
        builder.AppendLine(WriteExplanation("    med, median", "Evaluate the expression once with the median of each roll"));
        builder.AppendLine();
        builder.AppendLine("Make a dice roll:");
        builder.AppendLine(WriteExplanation("<rolls>d<sides>", "Roll <rolls> dice with <sides> sides and sum them together -> 4d6"));
        builder.AppendLine(WriteExplanation("<rolls>d[<min>,<max>]", "Roll <rolls> dice with <min> to <max> sides and sum them together -> 4d[1,6]"));
        builder.AppendLine();
        builder.AppendLine("Modifiers:");
        builder.AppendLine(WriteExplanation("k[<keep>]", "Keep the highest <keep> (default 1) -> 4d6k3"));
        builder.AppendLine(WriteExplanation("kl[<keep>]", "Keep the lowest <keep> (default 1) -> 4d6kl3"));
        builder.AppendLine(WriteExplanation("dh[<drop>]", "Drop the highest <drop> (default 1) -> 4d6dh3"));
        builder.AppendLine(WriteExplanation("dl[<drop>]", "Drop the highest <drop> (default 1) -> 4d6dl3"));
        builder.AppendLine(WriteExplanation("![<explode>]", "Explode dice up to <explode> number of times (default 1) -> 4d6!3"));
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