using System.Text.RegularExpressions;

namespace InvEntry.Gst.Core.Helpers
{
    public static class GstinHelper
    {
        private static readonly Regex GstinRegex =
            new(
                @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase);

        public static bool IsValid(string? gstin)
        {
            if (string.IsNullOrWhiteSpace(gstin))
                return false;

            gstin = gstin.Trim();

            if (gstin.Length != 15)
                return false;

            return GstinRegex.IsMatch(gstin);
        }

        public static string? GetStateCode(string? gstin)
        {
            if (!IsValid(gstin))
                return null;

            return gstin!.Substring(0, 2);
        }

        public static bool IsRegistered(string? gstin)
        {
            return IsValid(gstin);
        }

        public static string Normalize(string? gstin)
        {
            return gstin?.Trim().ToUpperInvariant()
                   ?? string.Empty;
        }
    }
}