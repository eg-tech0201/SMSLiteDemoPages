namespace External.ELMA.Client.Services;

public interface IElmaCircuitBreaker
{
    bool IsOpen();
    void RecordSuccess();
    void RecordFailure();
}
