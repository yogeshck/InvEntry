namespace InvEntry.Utils
{
    public class InvoiceNumberGenerator
    {
        private static string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string NUMERIC = "0123456789";
        private static Random random = new Random();

        public static string Generate(int alphalength = 3, int numbericLength = 5)
        {
            return string.Concat(RandomString(ALPHABET, alphalength), RandomString(NUMERIC, numbericLength));
        }

        public static string RandomString(string chars, int length)
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
