using DataAccess.Models;
using DataAccess.Repository;

namespace DataAccess.Services
{
    /// <summary>
    /// Provides centralized document/voucher number generation
    /// using the configuration maintained in VOUCHER_TYPES.
    ///
    /// IMPORTANT:
    /// This service only updates the tracked VoucherType entity.
    /// It does NOT call SaveChanges and does NOT create/commit
    /// a database transaction.
    ///
    /// The calling workflow is responsible for saving and committing
    /// the complete business transaction.
    /// </summary>
    public sealed class VoucherNumberService : IVoucherNumberService
    {
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepository;

        public VoucherNumberService(
            IRepositoryBase<VoucherType> voucherTypeRepository)
        {
            _voucherTypeRepository =
                voucherTypeRepository
                ?? throw new ArgumentNullException(
                    nameof(voucherTypeRepository));
        }

        /// <summary>
        /// Generates the next document number for the supplied
        /// VoucherType.DocumentType.
        ///
        /// Example:
        ///
        /// DocumentType   = "OM Purchase"
        /// DocNbrPrefix   = "OM"
        /// DocNbrLength   = 6
        /// LastUsedNumber = 125
        ///
        /// Result:
        /// OM000126
        ///
        /// LastUsedNumber will also be changed to 126 on the
        /// tracked VoucherType entity.
        /// </summary>
        public Task<string> GetNextNumberAsync(
            string documentType,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(documentType))
            {
                throw new ArgumentException(
                    "Document type is required.",
                    nameof(documentType));
            }

            documentType = documentType.Trim();

            // Retrieve VoucherType using the existing repository / EF ORM.
            //
            // The returned entity is expected to be tracked by EF Core,
            // therefore changing LastUsedNumber will be persisted when
            // the calling workflow executes SaveChanges.
            var voucherType =
                _voucherTypeRepository.Get(
                    x => x.DocumentType == documentType);

            if (voucherType is null)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{documentType}' is not configured.");
            }

            // ---------------------------------------------------------
            // Validate configuration
            // ---------------------------------------------------------

            if (voucherType.IsActive != true)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{documentType}' is inactive.");
            }

            var documentNumberLength =
                voucherType.DocNbrLength.GetValueOrDefault();

            if (documentNumberLength <= 0)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{documentType}' has an invalid " +
                    "document number length.");
            }

            var prefix =
                voucherType.DocNbrPrefix?.Trim()
                ?? string.Empty;

            // ---------------------------------------------------------
            // Calculate next running number
            // ---------------------------------------------------------

            var currentNumber =
                voucherType.LastUsedNumber.GetValueOrDefault();

            if (currentNumber < 0)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{documentType}' has an invalid " +
                    $"LastUsedNumber value '{currentNumber}'.");
            }

            var nextNumber =
                checked(currentNumber + 1);

            // ---------------------------------------------------------
            // Update tracked VoucherType
            // ---------------------------------------------------------

            voucherType.LastUsedNumber =
                nextNumber;

            // ---------------------------------------------------------
            // Format document number
            // ---------------------------------------------------------

            var formattedNumber =
                nextNumber.ToString(
                    $"D{documentNumberLength}");

            var documentNumber =
                $"{prefix}{formattedNumber}";

            return Task.FromResult(documentNumber);
        }
    }
}