using SMSLiteStaticDemo.Models.Workflows;

namespace SMSLiteStaticDemo.Services.Contracts;

public interface IGlobalSearchService
{
    Task<IReadOnlyList<GlobalSearchResultDto>> SearchAsync(
        GlobalSearchQueryDto query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedSearchDto>> GetSavedSearchesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task SaveSearchAsync(
        string userId,
        SavedSearchDto search,
        CancellationToken cancellationToken = default);
}
