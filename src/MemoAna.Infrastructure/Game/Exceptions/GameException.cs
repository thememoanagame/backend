using static Google.Apis.Requests.RequestError;

namespace MemoAna.Infrastructure.Game.Exceptions;

[Serializable]
public sealed class GameException : Exception
{
    internal int ErrorCode { get; init; }
    public GameException() : base()
    {
        ErrorCode = 400;
    }
    public GameException(string message) : base(message)
    {
        ErrorCode = 400;
    }
    public GameException(string message, int errorCode) : base(message)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(errorCode, 400, nameof(errorCode));
        ErrorCode = errorCode;
    }
    public GameException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = 400;
    }
    public GameException(string message, Exception innerException, int errorCode) : base(message, innerException)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(errorCode, 400, nameof(errorCode));
        ErrorCode = errorCode;
    }

    public int GetErrorCode() => ErrorCode;
}
