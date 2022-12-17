using Newtonsoft.Json.Linq;

namespace KuCoinLend
{
    public class GetAccData

    {
        public List<string?> tickers = new();
        private JObject jsonObj;

        public GetAccData()
        {

            List<string> unlendableList = new List<string>();
            using (var reader = new StreamReader(@"C:\Users\nick_\source\repos\KuCoinLend\unlendable.csv"))
            {

                List<string> listB = new List<string>();
                while (!reader.EndOfStream)
                {
                    string? line = reader.ReadLine();

                    unlendableList.Add($"{line}");
                }
            }

            string temp = KuCoinCall.get_results("/api/v1/accounts", "GET");

            jsonObj = JObject.Parse(temp);
            if ((string)jsonObj["code"] != "200000")
            {
                var currTime = DateTime.Now;
                string msg = "[" + currTime.ToString() + "]" + " " + temp;
                File.AppendAllText(@"C:\\Users\\nick_\\source\\repos\\KuCoinLend\\" + "trans_log.txt", msg + "\n");

                Console.WriteLine($"something went wrong. Response code {jsonObj["code"]}");
            }

            JArray ojObject = (JArray)jsonObj["data"];

            foreach (JToken item in ojObject)
            {

                if (!unlendableList.Contains(item.Value<string?>("currency")))  //doesn't exist in list
                {
                    if (!tickers.Contains(item.Value<string?>("currency")))     //doesn't exist in the unlendable list 
                    {
                        //Console.WriteLine("doesn't exist");
                        if (item.Value<string?>("type") == "main"
                            && (double.Parse(item.Value<string?>("balance")) > 0
                                || double.Parse(item.Value<string?>("available")) > 0
                                || double.Parse(item.Value<string?>("holds")) > 0))
                            tickers.Add(item.Value<string?>("currency"));
                    }
                }
            }
        }
    }
}
