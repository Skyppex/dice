namespace Dice;

public record Args(IEvaluationMode Mode, string Roll, bool PrintExpression, bool Timer)
{
    public static readonly Args Empty = new(new NoEvaluation(), string.Empty, false, false);
}

public class ArgsParser
{
    private readonly Stack<string> _args;
    
    public ArgsParser(string[] args) => _args = args.Reverse().ToStack();

    public Args ParseArgs() => GetEvaluationMode();
    
    private Args GetEvaluationMode()
    {
        if (_args.Count == 0)
        {
            Help.Print();
            return Args.Empty;
        }

        var argsContainer = new ArgsContainer();
        
        while (_args.TryPop(out string? arg))
        {
            switch (arg)
            {
                case "-h" or "-help" or "-?":
                    return Args.Empty;

                case "-m" or "-mode":
                    if (argsContainer.Mode != null)
                        throw new ArgumentException("Mode already specified");
                    
                    argsContainer.Mode = ParseModeArgs();
                    break;
                
                case "-e" or "-expression":
                    argsContainer.PrintExpression = true;
                    break;
                
                case "-t" or "-time":
                    argsContainer.Timer = true;
                    break;
                
                case var _ when !arg.StartsWith('-'):
                    _args.Push(arg);
                    argsContainer.Roll = string.Join(' ', _args);
                    _args.Clear();
                    break;
                
                default:
                    throw new ArgumentException($"Invalid argument: {arg}");
            }
        }
        
        if (string.IsNullOrEmpty(argsContainer.Roll))
            throw new ArgumentException("No roll specified");

        return new Args(argsContainer.Mode ?? new SingleEvaluation(), argsContainer.Roll, argsContainer.PrintExpression, argsContainer.Timer);
    }

    private IEvaluationMode? ParseModeArgs()
    {
        string arg = _args.Pop();

        switch (arg)
        {
            case var _ when arg.StartsWith("simavg"):
            {
                int indexOfColon = arg.IndexOf(':');
                int iterations;
                
                if (indexOfColon == -1)
                    iterations = 100;
                else
                {
                    var iterationsValue = arg[(indexOfColon + 1)..];
                    
                    if (!int.TryParse(iterationsValue, out iterations))
                        throw new ArgumentException($"Couldn't parse number of iterations as integer: {iterationsValue}");
                }
                
                return new SimulatedAverageEvaluation(iterations);
            }

            case "avg" or "average":
                return new CalculatedAverageEvaluation();
            
            case "max":
                return new MaximumEvaluation();
            
            case "min":
                return new MinimumEvaluation();
            
            case "med" or "median":
                return new MedianEvaluation();
            
            default:
                return new SingleEvaluation();
        }
    }
    
    private class ArgsContainer
    {
        public IEvaluationMode? Mode { get; set; }
        public string Roll { get; set; } = string.Empty;
        public bool PrintExpression { get; set; }
        public bool Timer { get; set; }
    }
}