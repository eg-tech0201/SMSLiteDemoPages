using sms_lite.Models.Workflows;

namespace sms_lite.Services.Contracts;

public interface IExportAndAccessService
{
    Task<UserAccessProfileDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<bool> CanExportAsync(
        string exportType,
        CancellationToken cancellationToken = default);

    Task<ExportResultDto> ExportAsync(
        ExportRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditTrailEntryDto>> GetAuditTrailAsync(
        CancellationToken cancellationToken = default);

    Task WriteAuditEntryAsync(
        AuditTrailEntryDto entry,
        CancellationToken cancellationToken = default);
}
