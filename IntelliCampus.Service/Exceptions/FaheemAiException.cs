namespace IntelliCampus.Service.Exceptions;

public sealed class FaheemAiException : Exception
{
    public int StatusCode { get; }
    public string? Signal { get; }

    public FaheemAiException(string message, int statusCode = 503, string? signal = null) : base(message)
    {
        StatusCode = statusCode;
        Signal = signal;
    }
}
