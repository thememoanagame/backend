namespace MemoAna.Domain.Exceptions;

public class CardNotFoundException(string? message = null!) : KeyNotFoundException(message ??_localizerKey)
{
    public static readonly string _localizerKey = "CardNotFoundEx";
}
