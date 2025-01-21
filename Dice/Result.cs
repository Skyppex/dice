using static Monads.Option;

namespace Monads;

public static class Result
{
    public static Result<T, TError> Ok<T, TError>(T value) => new(value);

    public static Result<T, TError> Error<T, TError>(TError error) => new(error);
}

public readonly struct Result<T, TError>
{
    internal Result(T value)
    {
        Value = value;
        Error = default!;
        IsOk = true;
    }

    internal Result(TError error)
    {
        Value = default!;
        Error = error;
        IsOk = false;
    }

    internal T Value { get; }
    internal TError Error { get; }
    internal bool IsOk { get; }
}

public static class ResultExtensions
{
    public static void Match<T, TError>(
        this Result<T, TError> result,
        Action<T> ok,
        Action<TError> error
    )
    {
        ArgumentNullException.ThrowIfNull(ok, nameof(ok));
        ArgumentNullException.ThrowIfNull(error, nameof(error));

        if (result.IsOk())
            ok(result.Value!);
        else
            error(result.Error!);
    }

    public static TResult Match<T, TError, TResult>(
        this Result<T, TError> result,
        Func<T, TResult> ok,
        Func<TError, TResult> error
    )
    {
        ArgumentNullException.ThrowIfNull(ok, nameof(ok));
        ArgumentNullException.ThrowIfNull(error, nameof(error));
        return result.IsOk() ? ok(result.Value!) : error(result.Error);
    }

    public static bool IsOk<T, TError>(this Result<T, TError> result) => result.IsOk;

    public static bool IsError<T, TError>(this Result<T, TError> result) => !result.IsOk();

    public static Option<T> Ok<T, TError>(this Result<T, TError> result) =>
        result.IsOk() ? Some(result.Value!) : None<T>();

    public static Result<TResult, TError> Map<T, TError, TResult>(
        this Result<T, TError> result,
        Func<T, TResult> mapper
    )
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        return result.IsOk()
            ? Result.Ok<TResult, TError>(mapper(result.Value!))
            : Result.Error<TResult, TError>(result.Error);
    }

    public static Result<T, TErrorResult> MapError<T, TError, TErrorResult>(
        this Result<T, TError> result,
        Func<TError, TErrorResult> mapper
    )
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        return result.IsOk()
            ? Result.Ok<T, TErrorResult>(result.Value!)
            : Result.Error<T, TErrorResult>(mapper(result.Error));
    }

    public static TResult MapOr<T, TError, TResult>(
        this Result<T, TError> result,
        TResult defaultValue,
        Func<T, TResult> mapper
    )
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        return result.IsOk() ? mapper(result.Value!) : defaultValue;
    }

    public static TResult MapOrElse<T, TError, TResult>(
        this Result<T, TError> result,
        Func<TResult> defaultGetter,
        Func<T, TResult> mapper
    )
    {
        ArgumentNullException.ThrowIfNull(defaultGetter, nameof(defaultGetter));
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        return result.IsOk() ? mapper(result.Value!) : defaultGetter();
    }

    public static Result<T2, TError> And<T1, T2, TError>(
        this Result<T1, TError> result,
        Result<T2, TError> resultB
    ) => result.IsOk() ? resultB : Result.Error<T2, TError>(result.Error);

    public static Result<T2, TError> AndThen<T1, T2, TError>(
        this Result<T1, TError> result,
        Func<T1, Result<T2, TError>> func
    )
    {
        ArgumentNullException.ThrowIfNull(func, nameof(func));
        return result.IsOk() ? func(result.Value!) : Result.Error<T2, TError>(result.Error);
    }

    public static Result<T, TErrorResult> Or<T, TError, TErrorResult>(
        this Result<T, TError> result,
        Result<T, TErrorResult> resultB
    ) => result.IsOk() ? Result.Ok<T, TErrorResult>(result.Value!) : resultB;

    public static Result<T, TErrorResult> OrElse<T, TError, TErrorResult>(
        this Result<T, TError> result,
        Func<Result<T, TErrorResult>> func
    )
    {
        ArgumentNullException.ThrowIfNull(func, nameof(func));
        return result.IsOk() ? Result.Ok<T, TErrorResult>(result.Value!) : func();
    }

    public static Option<Result<T, TError>> Transpose<T, TError>(
        this Result<Option<T>, TError> result
    )
    {
        return result.Match(
            ok: option =>
                option.Match(
                    some: v => Some(Result.Ok<T, TError>(v)),
                    none: None<Result<T, TError>>
                ),
            error: e => Some(Result.Error<T, TError>(e))
        );
    }

    public static T Unwrap<T, TError>(this Result<T, TError> result)
    {
        if (result.IsOk())
            return result.Value!;

        if (result.Error is Exception exception)
            throw new InvalidOperationException("Called expect on an Error value.", exception);

        throw new InvalidOperationException("Called expect on an Error value.");
    }

    public static TError UnwrapError<T, TError>(this Result<T, TError> result)
    {
        if (result.IsOk())
            throw new InvalidOperationException("Called unwrap error on an Ok value.");

        return result.Error;
    }

    public static T UnwrapOr<T, TError>(this Result<T, TError> result, T defaultValue) =>
        result.IsOk() ? result.Value! : defaultValue;

    public static T UnwrapOrElse<T, TError>(this Result<T, TError> result, Func<T> defaultGetter) =>
        result.IsOk() ? result.Value! : defaultGetter();

    public static T Expect<T, TError>(this Result<T, TError> result, string message)
    {
        if (result.IsOk())
            return result.Value!;

        if (result.Error is Exception exception)
            throw new InvalidOperationException(
                $"Called expect on an Error value. {message}",
                exception
            );

        throw new InvalidOperationException($"Called expect on an Error value. {message}");
    }

    public static IEnumerator<T> Iterable<T, TError>(this Result<T, TError> result)
    {
        if (result.IsOk())
            yield return result.Value;
    }

    /// <summary>
    /// Returns the first Ok value in the sequence as a Some value, or None if there are no Ok values.
    /// </summary>
    public static Option<T> Coalesce<T, TError>(this IEnumerable<Result<T, TError>> results)
    {
        foreach (Result<T, TError> result in results)
            if (result.IsOk())
                return Some(result.Value);

        return None<T>();
    }

    public static Result<T, TError> Flatten<T, TError>(
        this Result<Result<T, TError>, TError> result
    ) => result.Match(ok: r => r, error: Result.Error<T, TError>);

    public static IEnumerable<T> Flatten<T, TError>(this IEnumerable<Result<T, TError>> results) =>
        results.Where(result => result.IsOk()).Select(result => result.Value!);
}
