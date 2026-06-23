namespace SMSLiteUI.Components.Dashboard;

public partial class SurveyCollectionProgressWidget
{
    private static readonly IReadOnlyList<SurveyProgressGridRow> SurveyProgressRows = [];

    private sealed record SurveyProgressGridRow(
        string SurveyName,
        DateTime? SurveyDate,
        int? SampleSize,
        int? CheckedIn,
        decimal? CheckedInPercent,
        int? DaysRemaining);
}
