namespace InstaFakeAnalyzer.Services;

public sealed class ProviderResponseException : InvalidOperationException
{
    public ProviderResponseException(string message) : base(message)
    {
    }

    public ProviderResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
