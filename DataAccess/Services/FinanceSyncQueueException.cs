namespace DataAccess.Services;

public sealed class FinanceSyncQueueException : Exception
{
    public FinanceSyncQueueException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
