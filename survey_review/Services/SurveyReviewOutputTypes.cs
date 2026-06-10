namespace SMS.Integration.SurveyReview.Services;

public static class SurveyReviewOutputTypes
{
    public static string GetName(int outputType)
        => outputType switch
        {
            1 => "RTF",
            2 => "PDF",
            3 => "JSON",
            4 => "PDF Booklet",
            _ => "Output"
        };

    public static string GetFormat(int outputType)
        => outputType switch
        {
            1 => "RTF",
            2 => "PDF",
            3 => "JSON",
            4 => "PDF",
            _ => "BIN"
        };

    public static string GetContentType(int outputType)
        => outputType switch
        {
            1 => "application/rtf",
            2 => "application/pdf",
            3 => "application/json",
            4 => "application/pdf",
            _ => "application/octet-stream"
        };

    public static string GetExtension(int outputType)
        => outputType switch
        {
            1 => "rtf",
            2 => "pdf",
            3 => "json",
            4 => "pdf",
            _ => "bin"
        };
}
