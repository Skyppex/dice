using System.Text;

namespace Dice;

public static class Help
{
    public static void Print()
    {
        StringBuilder builder = new();
        builder.AppendLine("usage: dice <expression>");
        builder.AppendLine();
        builder.AppendLine("make a dice roll");
        builder.AppendLine(WriteExplanation("<rolls>d<sides>", "Roll <rolls> dice with <sides> sides and sums them together -> 4d6"));
        builder.AppendLine();
        builder.AppendLine("modifiers:");
        builder.AppendLine(WriteExplanation("k[<keep>]", "Keep the highest <keep> (default 1) -> 4d6k3"));
        builder.AppendLine(WriteExplanation("kl[<keep>]", "Keep the lowest <keep> (default 1) -> 4d6kl3"));
        builder.AppendLine(WriteExplanation("dh[<drop>]", "Drop the highest <drop> (default 1) -> 4d6dh3"));
        builder.AppendLine(WriteExplanation("dl[<drop>]", "Drop the highest <drop> (default 1) -> 4d6dl3"));
        builder.AppendLine(WriteExplanation("![<explode>]", "Explode dice up to <explode> number of times (default 1) -> 4d6!3"));
        builder.AppendLine(WriteExplanation("%", "Replaced by the number 100 (useful for percentile dice) -> 1d%"));
        builder.AppendLine();
        builder.AppendLine("basic math operators:");
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
        const int PADDING = 20;
        int padding = PADDING - expressionLength;
        return $"    {expression}{new string(' ', padding)}{explanation}";
    }
}