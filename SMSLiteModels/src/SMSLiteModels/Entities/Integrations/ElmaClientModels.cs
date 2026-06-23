namespace SMSLiteModels.Entities.Integrations;

public enum ElmaAuthenticationMode
{
    None = 0,
    ApiKey = 1,
    BearerToken = 2
}

public enum ElmaTransactionStatus
{
    Pending = 0,
    Submitted = 1,
    Completed = 2,
    Failed = 3,
    Unavailable = 4
}

public sealed record ElmaContactUpdateRequest(
    string Poid,
    string? RespondentId,
    string? FirstName,
    string? LastName,
    string? OperationName,
    string? PhoneNumber,
    string? EmailAddress,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? ZipCode,
    string? Notes,
    string RequestedBy,
    DateTime RequestedAtUtc
);

public sealed record ElmaSubmitTransactionRequest(
    ElmaContactUpdateRequest ContactUpdate,
    string SourceSystem,
    string CorrelationId
);

public sealed record ElmaSubmitTransactionResponse(
    string TransactionId,
    ElmaTransactionStatus Status,
    string ConfirmationMessage,
    DateTime SubmittedAtUtc,
    string? ExternalReference
);

public sealed record ElmaTransactionStatusResponse(
    string TransactionId,
    ElmaTransactionStatus Status,
    string UserMessage,
    DateTime LastUpdatedUtc,
    string? ExternalReference,
    string? FailureReason
);

public sealed record ElmaFoUpdateLinkResponse(
    Uri UpdateRequestUri,
    string LinkMode,
    string UserMessage
);

public sealed record ElmaFoUpdateLinkRequest(
    string? Poid,
    string? StateId,
    string? FrameId,
    string? SourceSystem,
    string? CorrelationId
);

public sealed record ElmaErrorResponse(
    string Code,
    string Message,
    string? Detail,
    string? CorrelationId
);

public sealed record ElmaClientCapabilityResponse(
    ElmaAuthenticationMode AuthenticationMode,
    TimeSpan DownstreamTimeout,
    bool CircuitBreakerEnabled,
    bool SupportsFormPrefill,
    string Notes
);
