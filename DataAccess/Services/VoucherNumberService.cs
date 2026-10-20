using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Services
{
    /// <summary>
    /// Provides centralized document/voucher number generation
    /// using the configuration maintained in VOUCHER_TYPES.
    ///
    /// The running number is taken with a single atomic
    /// UPDATE ... SET LAST_USED_NUMBER = LAST_USED_NUMBER + 1, so two
    /// requests can never receive the same number. Inside the caller's
    /// transaction the updated VOUCHER_TYPES row stays locked until that
    /// transaction commits, and a rollback returns the number. Without a
    /// caller transaction the increment is committed on its own.
    /// </summary>
    public sealed class VoucherNumberService : IVoucherNumberService
    {
        private readonly MijmsContext _context;

        public VoucherNumberService(
            MijmsContext context)
        {
            _context =
                context
                ?? throw new ArgumentNullException(
                    nameof(context));
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
        /// LastUsedNumber is 126 in the database afterwards.
        /// </summary>
        public async Task<string> GetNextNumberAsync(
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

            var voucherTypes =
                await _context.VoucherTypes
                    .AsNoTracking()
                    .Where(x => x.DocumentType == documentType)
                    .Take(2)
                    .ToListAsync(cancellationToken);

            if (voucherTypes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{documentType}' is not configured.");
            }

            if (voucherTypes.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{documentType}' is configured more than once.");
            }

            var voucherType = voucherTypes[0];

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

            if (voucherType.LastUsedNumber.GetValueOrDefault() < 0)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{documentType}' has an invalid " +
                    $"LastUsedNumber value '{voucherType.LastUsedNumber}'.");
            }

            var prefix =
                voucherType.DocNbrPrefix?.Trim()
                ?? string.Empty;

            // ---------------------------------------------------------
            // Take the next running number atomically
            // ---------------------------------------------------------

            var nextNumber =
                await IncrementAsync(
                    voucherType.Gkey,
                    documentType,
                    cancellationToken);

            // ---------------------------------------------------------
            // Keep a tracked VoucherType (if any) in step, so a later
            // SaveChanges never writes an older number back
            // ---------------------------------------------------------

            var tracked =
                _context.VoucherTypes.Local
                    .FirstOrDefault(x => x.Gkey == voucherType.Gkey);

            if (tracked is not null)
            {
                var property =
                    _context.Entry(tracked)
                        .Property(x => x.LastUsedNumber);

                property.CurrentValue = nextNumber;
                property.OriginalValue = nextNumber;
                property.IsModified = false;
            }

            // ---------------------------------------------------------
            // Format document number
            // ---------------------------------------------------------

            var formattedNumber =
                nextNumber.ToString(
                    $"D{documentNumberLength}");

            return $"{prefix}{formattedNumber}";
        }

        private async Task<int> IncrementAsync(
            int voucherTypeGkey,
            string documentType,
            CancellationToken cancellationToken)
        {
            // The UPDATE and the read must see the same row version, so
            // they run in one transaction: the caller's, or a short one.
            IDbContextTransaction? ownTransaction =
                _context.Database.CurrentTransaction is null
                    ? await _context.Database.BeginTransactionAsync(cancellationToken)
                    : null;

            try
            {
                var updated =
                    await _context.VoucherTypes
                        .Where(x => x.Gkey == voucherTypeGkey)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(
                                x => x.LastUsedNumber,
                                x => (x.LastUsedNumber ?? 0) + 1),
                            cancellationToken);

                if (updated != 1)
                {
                    throw new InvalidOperationException(
                        $"Voucher type '{documentType}' could not be numbered.");
                }

                var nextNumber =
                    await _context.VoucherTypes
                        .AsNoTracking()
                        .Where(x => x.Gkey == voucherTypeGkey)
                        .Select(x => x.LastUsedNumber)
                        .SingleAsync(cancellationToken);

                if (ownTransaction is not null)
                {
                    await ownTransaction.CommitAsync(cancellationToken);
                }

                return nextNumber.GetValueOrDefault();
            }
            catch
            {
                if (ownTransaction is not null)
                {
                    await ownTransaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                if (ownTransaction is not null)
                {
                    await ownTransaction.DisposeAsync();
                }
            }
        }
    }
}
