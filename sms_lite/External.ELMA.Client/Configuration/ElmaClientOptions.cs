using sms_lite.Models.Integrations;

namespace External.ELMA.Client.Configuration;

public sealed class ElmaClientOptions
{
    public const string SectionName = "ElmaClient";

    public string BaseUrl { get; set; } = "https://elma.nass.usda.gov/";
    public string FoUpdateRequestPath { get; set; } = "FO/FOUpdateRequest.aspx";
    public ElmaAuthenticationMode AuthenticationMode { get; set; } = ElmaAuthenticationMode.ApiKey;
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public string ApiKey { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public int DownstreamTimeoutSeconds { get; set; } = 5;
    public int CircuitBreakerFailureThreshold { get; set; } = 3;
    public int CircuitBreakerBreakSeconds { get; set; } = 30;
    public bool SupportsFormPrefill { get; set; }
    public string SourceSystem { get; set; } = "SCT";
    public string PoidQueryParameterName { get; set; } = "poid";
    public string StateIdQueryParameterName { get; set; } = "stateId";
    public string FrameIdQueryParameterName { get; set; } = "frameId";
    public string SourceSystemQueryParameterName { get; set; } = "sourceSystem";
    public string CorrelationIdQueryParameterName { get; set; } = "correlationId";
}
