namespace KuCoinLend
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int deleteOrdersOlderThanHours = 1;

            GetAccData accdata = new();

            foreach (string s in accdata.tickers)
            {
                Console.WriteLine($"processing {s}");
                Lend coin = new Lend(deleteOrdersOlderThanHours, s);

            }

            Console.WriteLine("finito");

        }
    }
}
