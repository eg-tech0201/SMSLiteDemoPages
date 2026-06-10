namespace External.ELMA.Client.Services;

public sealed class ElmaGatewayException : Exception
{
    public ElmaGatewayException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
