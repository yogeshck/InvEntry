namespace InvEntry.Utils
{
    public class InvoiceNumberGenerator
    {
        private static string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string NUMERIC = "0123456789";
        private static Random random = new Random();

        private static string FileName = "counter.json";

        private static Counter? Counter;

        public static string Generate()
        {
            if(Counter is null)
            {
                Counter = JsonReadWrite.ReadJson<Counter>(FileName, new());
            }

            Counter.Count++;

            JsonReadWrite.WriteJson(FileName, Counter);
            return string.Concat("B", Counter.Count.ToString());
        }

        public static string RandomString(string chars, int length)
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public class Counter
    {
        public int Count { get; set; } = 1000;
    }
}
