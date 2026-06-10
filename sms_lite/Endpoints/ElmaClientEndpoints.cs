using External.ELMA.Client.Services;
using Microsoft.AspNetCore.Mvc;
using SMSLiteStaticDemo.Models.Integrations;
using SMSLiteStaticDemo.Services.Contracts.Integration;

namespace SMSLiteStaticDemo.Server.Endpoints;

public static class ElmaClientEndpoints
{
    public static IEndpointRouteBuilder MapElmaClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/elma")
            .RequireAuthorization()
            .WithTags("ELMA Client");

        group.MapGet("/capabilities", async (IElmaGateway gateway, CancellationToken cancellationToken) =>
            Results.Ok(await gateway.GetCapabilitiesAsync(cancellationToken)))
            .WithName("GetElmaCapabilities")
            .WithSummary("Get ELMA client capabilities")
            .WithDescription("Returns the configured ELMA authentication mode, timeout, circuit breaker behavior, and current form-link support notes.");

        group.MapGet("/fo-update-link", async (
            [FromQuery] string? poid,
            [FromQuery] string? stateId,
            [FromQuery] string? frameId,
            [FromQuery] string? correlationId,
            IElmaGateway gateway,
            CancellationToken cancellationToken) =>
            Results.Ok(await gateway.GetFoUpdateRequestLinkAsync(
                new ElmaFoUpdateLinkRequest(poid, stateId, frameId, "SMS Lite", correlationId),
                cancellationToken)))
            .WithName("GetElmaFoUpdateLink")
            .WithSummary("Get ELMA FO Update Request link")
            .WithDescription("Returns the approved ELMA FO Update Request URL with respondent identifiers appended for static form prefill.");

        group.MapPost("/transactions", SubmitTransactionAsync)
            .WithName("SubmitElmaTransaction")
            .WithSummary("Submit ELMA transaction")
            .WithDescription("Accepts a contact update transaction and returns immediate confirmation from the ELMA client shell.");

        group.MapGet("/transactions/{transactionId}", GetTransactionStatusAsync)
            .WithName("GetElmaTransactionStatus")
            .WithSummary("Get ELMA transaction status")
            .WithDescription("Retrieves the latest known status for a submitted ELMA transaction.");

        return endpoints;
    }

    private static async Task<IResult> SubmitTransactionAsync(
        [FromBody] ElmaSubmitTransactionRequest request,
        IElmaGateway gateway,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await gateway.SubmitTransactionAsync(request, cancellationToken));
        }
        catch (ElmaGatewayException ex)
        {
            loggerFactory.CreateLogger("ELMA").LogWarning(ex, "ELMA submit failed.");
            return Results.Problem(
                title: "ELMA submission failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
    }

    private static async Task<IResult> GetTransactionStatusAsync(
        string transactionId,
        IElmaGateway gateway,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await gateway.GetTransactionStatusAsync(transactionId, cancellationToken));
        }
        catch (ElmaGatewayException ex)
        {
            loggerFactory.CreateLogger("ELMA").LogWarning(ex, "ELMA status lookup failed.");
            return Results.Problem(
                title: "ELMA status lookup failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
    }
}
