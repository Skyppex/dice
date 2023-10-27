using static Monads.Option;

namespace Monads;

public static class Option
{
    public static Option<T> Some<T>(T value) => new Option<T>(value);
    public static Option<T> None<T>() => Option<T>.None;
}

public readonly struct Option<T>
{
    public static readonly Option<T> None = new Option<T>();

    internal Option(T value)
    {
        Value = value;
        HasValue = value is not null;
    }

    internal T? Value { get; }
    internal bool HasValue { get; }
}

public static class OptionExtensions
{
    public static TResult Match<T, TResult>(this Option<T> option, Func<T, TResult> some, Func<TResult> none)
    {
        ArgumentNullException.ThrowIfNull(some, nameof(some));
        ArgumentNullException.ThrowIfNull(none, nameof(none));
        return option.IsSome() ? some(option.Value!) : none();
    }
    
    public static void Match<T>(this Option<T> option, Action<T> some, Action none)
    {
        ArgumentNullException.ThrowIfNull(some, nameof(some));
        ArgumentNullException.ThrowIfNull(none, nameof(none));

        if (option.IsSome())
            some(option.Value!);
        else
            none();
    }
    
    public static bool IsSome<T>(this Option<T> option) => option.HasValue;
    public static bool IsNone<T>(this Option<T> option) => !option.IsSome();

    public static void IfSomeRun<T>(this Option<T> option, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));
        
        if (!option.HasValue)
            return;

        action(option.Value!);
    }
    
    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));

        return option.HasValue
            ? Some(mapper(option.Value!))
            : None<TResult>();
    }

    public static TResult MapOr<T, TResult>(this Option<T> option, TResult defaultValue, Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        return option.IsSome() ? mapper(option.Value!) : defaultValue;
    }

    public static TResult MapOrElse<T, TResult>(this Option<T> option, Func<TResult> defaultGetter, Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(defaultGetter, nameof(defaultGetter));
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        return option.IsSome() ? mapper(option.Value!) : defaultGetter();
    }

    public static Option<TResult> And<T, TResult>(this Option<T> option, Option<TResult> optionB) =>
        option.IsSome() ? optionB : None<TResult>();
    
    public static Option<TResult> AndThen<T, TResult>(this Option<T> option, Func<T, Option<TResult>> func)
    {
        ArgumentNullException.ThrowIfNull(func, nameof(func));
        return option.IsSome() ? func(option.Value!) : None<TResult>();
    }
    
    public static bool IsSomeAnd<T>(this Option<T> option, Predicate<T> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        return option.IsSome() && predicate(option.Value!);
    }

    public static Option<T> Or<T>(this Option<T> option, Option<T> optionB) => option.IsSome() ? option : optionB;

    public static Option<T> OrElse<T>(this Option<T> option, Func<Option<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func, nameof(func));
        return option.IsSome() ? option : func();
    }

    public static Option<T> Xor<T>(this Option<T> option, Option<T> optionB)
    {
        return (option.IsSome(), optionB.IsSome()) switch
        {
            (true, false) => option,
            (false, true) => optionB,
            _ => None<T>()
        };
    }

    public static Result<T, TError> OkOr<T, TError>(this Option<T> option, TError error) where TError : Exception => 
        option.MapOr(Result.Error<T, TError>(error), Result.Ok<T, TError>);

    public static Result<T, TError> OkOrElse<T, TError>(this Option<T> option, Func<TError> errorGetter) where TError : Exception
    {
        ArgumentNullException.ThrowIfNull(errorGetter, nameof(errorGetter));
        return option.MapOr(Result.Error<T, TError>(errorGetter()), Result.Ok<T, TError>);
    }

    public static Result<Option<T>, TError> Transpose<T, TError>(this Option<Result<T, TError>> option, TError error)
        where TError : Exception
    {
        return option.Match(
            some: result => result.Match(
                ok: v => Result.Ok<Option<T>, TError>(Some(v)),
                error: e => Result.Error<Option<T>, TError>(e)),
            none: () => Result.Ok<Option<T>, TError>(None<T>()));
    }

    public static T Unwrap<T>(this Option<T> option)
    {
        if (option.IsNone())
            throw new InvalidOperationException("Called unwrap on a None value.");
        
        return option.Value!;
    }

    public static T UnwrapOr<T>(this Option<T> option, T defaultValue) =>
        option.IsSome() ? option.Value! : defaultValue;

    public static T UnwrapOrElse<T>(this Option<T> option, Func<T> defaultGetter)
    {
        ArgumentNullException.ThrowIfNull(defaultGetter, nameof(defaultGetter));
        return option.IsSome() ? option.Value! : defaultGetter();
    }

    public static T Expect<T>(this Option<T> option, string message)
    {
        return option.Match(
            some: v => v,
            none: () => throw new InvalidOperationException($"Called expect on a None value. {message}"));
    }
    
    public static Option<(T1, T2)> Zip<T1, T2>(this Option<T1> option, Option<T2> other) =>
        option.And(other).IsSome() ? Some((option.Value!, other.Value!)) : None<(T1, T2)>();

    public static Option<TResult> ZipWith<T1, T2, TResult>(this Option<T1> option, Option<T2> other,
        Func<T1, T2, TResult> zipper)
    {
        ArgumentNullException.ThrowIfNull(zipper, nameof(zipper));
        return option.And(other).IsSome() ? Some(zipper(option.Value!, other.Value!)) : None<TResult>();
    }

    public static (Option<T1>, Option<T2>) Unzip<T1, T2>(this Option<(T1, T2)> option) =>
        option.Match(
            some: v => (Some(v.Item1), Some(v.Item2)),
            none: () => (None<T1>(), None<T2>()));

    public static Option<T> Filter<T>(this Option<T> option, Predicate<T> predicate)
    {
        return option.Match(
            some: v => predicate(v) ? option : None<T>(),
            none: None<T>);
    }
    
    public static IEnumerator<T> Iterable<T>(this Option<T> option)
    {
        if (option.IsSome())
            yield return option.Value!;
    }

    /// <summary>
    /// Returns the first Some value in the sequence, or None if there are no Some values.
    /// </summary>
    public static Option<T> Coalesce<T>(this IEnumerable<Option<T>> options)
    {
        foreach (Option<T> option in options)
            if (option.IsSome())
                return option;

        return None<T>();
    }

    public static Option<T> Flatten<T>(this Option<Option<T>> option) => option.Match(
        some: v => v,
        none: None<T>);

    public static IEnumerable<T> Flatten<T>(this IEnumerable<Option<T>> options) => options
        .Where(option => option.IsSome())
        .Select(option => option.Value!);
}
