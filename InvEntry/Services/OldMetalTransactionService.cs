using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IOldMetalTransactionService
    {
        Task<OldMetalTransaction>
            GetOldMetalTransaction(
                string voucherId);

        Task<OldMetalTransaction>
            CreateOldMetalTransaction(
                OldMetalTransaction oldMetalTransaction);

        Task UpdateOldMetalTransaction(
            OldMetalTransaction oldMetalTransaction);

        Task<IEnumerable<OldMetalTransaction>>
            GetByDocRefNbr(
                string docRefNbr);

        Task<string>
            CreateOldMetalTransaction(
                IEnumerable<OldMetalTransaction> lines);

        Task<IEnumerable<OldMetalTransaction>>
            GetAll(
                DateSearchOption options);
    }


    public class OldMetalTransactionService
        : IOldMetalTransactionService
    {
        private readonly IMijmsApiService
            _mijmsApiService;


        public OldMetalTransactionService(
            IMijmsApiService mijmsApiService)
        {
            _mijmsApiService =
                mijmsApiService;
        }


        // ============================================================
        // CREATE SINGLE
        // ============================================================

        public async Task<OldMetalTransaction>
            CreateOldMetalTransaction(
                OldMetalTransaction oldMetalTransaction)
        {
            ArgumentNullException.ThrowIfNull(
                oldMetalTransaction);

            return await _mijmsApiService
                .Post(
                    "api/OldMetalTransaction/",
                    oldMetalTransaction);
        }


        // ============================================================
        // CREATE COMPLETE OLD METAL PURCHASE
        //
        // One HTTP request.
        // One transaction number.
        // ============================================================

        public async Task<string>
            CreateOldMetalTransaction(
                IEnumerable<OldMetalTransaction> lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            var transactionLines =
                lines.ToList();

            if (transactionLines.Count == 0)
            {
                throw new InvalidOperationException(
                    "No old metal transaction lines were supplied.");
            }

            /*
             * IMPORTANT:
             *
             * Generic order is:
             *
             * <TRequest, TResponse>
             *
             * Request:
             * List<OldMetalTransaction>
             *
             * Response:
             * string transaction number
             */
            return await _mijmsApiService
                .Post<List<OldMetalTransaction>, string>(
                    "api/OldMetalTransaction/batch",
                    transactionLines);
        }


        // ============================================================
        // GET BY TRANSACTION NUMBER
        // ============================================================

        public async Task<OldMetalTransaction>
            GetOldMetalTransaction(
                string transNbr)
        {
            if (string.IsNullOrWhiteSpace(
                    transNbr))
            {
                return null!;
            }

            return await _mijmsApiService
                .Get<OldMetalTransaction>(
                    $"api/OldMetalTransaction/{Uri.EscapeDataString(transNbr)}");
        }


        // ============================================================
        // GET BY DOCUMENT REFERENCE
        // ============================================================

        public async Task<IEnumerable<OldMetalTransaction>>
            GetByDocRefNbr(
                string docRefNbr)
        {
            if (string.IsNullOrWhiteSpace(
                    docRefNbr))
            {
                return Enumerable
                    .Empty<OldMetalTransaction>();
            }

            return await _mijmsApiService
                .GetEnumerable<OldMetalTransaction>(
                    $"api/OldMetalTransaction/docRefNbr/{Uri.EscapeDataString(docRefNbr)}");
        }


        // ============================================================
        // UPDATE
        // ============================================================

        public async Task UpdateOldMetalTransaction(
            OldMetalTransaction oldMetalTransaction)
        {
            ArgumentNullException.ThrowIfNull(
                oldMetalTransaction);

            await _mijmsApiService
                .Put(
                    "api/OldMetalTransaction/",
                    oldMetalTransaction);
        }


        // ============================================================
        // FILTER
        // ============================================================

        public async Task<IEnumerable<OldMetalTransaction>>
            GetAll(
                DateSearchOption options)
        {
            ArgumentNullException.ThrowIfNull(
                options);

            return await _mijmsApiService
                .PostEnumerable<
                    OldMetalTransaction,
                    DateSearchOption>(
                    "api/OldMetalTransaction/filter",
                    options);
        }
    }
}