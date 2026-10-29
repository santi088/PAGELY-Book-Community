namespace BookCommunity.Api.Services;

public sealed class OpenLibraryException : Exception
{
    public int StatusCode { get; }
    public int? RetryAfterSeconds { get; }

    public OpenLibraryException(int statusCode, string message, int? retryAfterSeconds = null) : base(message)
    {
        StatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
