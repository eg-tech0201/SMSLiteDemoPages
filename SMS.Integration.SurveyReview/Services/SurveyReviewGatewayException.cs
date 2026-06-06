namespace SMS.Integration.SurveyReview.Services;

public sealed class SurveyReviewGatewayException : Exception
{
    public SurveyReviewGatewayException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
