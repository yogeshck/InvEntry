namespace DataAccess.Services
{
    public interface IVoucherNumberService
    {
        Task<string> GetNextNumberAsync(
            string documentType,
            CancellationToken cancellationToken = default);
    }
}