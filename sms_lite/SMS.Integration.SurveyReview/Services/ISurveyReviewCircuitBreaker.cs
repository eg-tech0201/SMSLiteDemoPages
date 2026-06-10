namespace SMS.Integration.SurveyReview.Services;

public interface ISurveyReviewCircuitBreaker
{
    bool IsOpen();
    void RecordSuccess();
    void RecordFailure();
}
