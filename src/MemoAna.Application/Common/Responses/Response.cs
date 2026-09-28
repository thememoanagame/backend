namespace MemoAna.Application.Common.Responses;

/// <summary>Represents an application response.</summary>
/// <typeparam name="T">The response data type.</typeparam>
/// <param name="Succeeded">Whether the operation succeeded.</param>
/// <param name="Data">The operation data.</param>
/// <param name="Errors">The operation errors.</param>
public sealed record Response<T>(
    bool Succeeded,
    T? Data,
    IReadOnlyCollection<string> Errors);

/// <summary>Represents an application response.</summary>
/// <param name="Errors">The operation errors.</param>
/// <param name="Exception">The exception thrown.</param>
public sealed record Response(IReadOnlyCollection<string> Errors, Exception? Exception = null!);


/// <summary>Creates application responses.</summary>
public static class ResponseMaker
{
    /// <summary>Creates a successful response.</summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="data">The response data.</param>
    /// <returns>A successful response.</returns>
    public static Response<T> Success<T>(T data)
    {
        return new Response<T>(
            true,
            data,
            []);
    }

    /// <summary>Creates a failed response.</summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed response.</returns>
    public static Response<T> Failure<T>(
        IEnumerable<string> errors)
    {
        return new Response<T>(
            false,
            default,
            [.. errors]);
    }

    /// <summary>Creates a failed response.</summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed response.</returns>
    public static Response<T> Failure<T>(
        params string[] errors)
    {
        return new Response<T>(false, default!, errors);
    }
    /// <summary>Creates a failed response.</summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed response.</returns>
    public static Response Failure(
        params string[] errors)
    {
        return new Response(errors);
    }
}
