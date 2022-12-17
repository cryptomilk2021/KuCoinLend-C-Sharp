using Newtonsoft.Json.Linq;
using System.Collections;

namespace KuCoinLend
{
    class Lend
    {
        public string ticker;
        public int hoursToLive;
        string baseWorkDir = "C:\\Users\\nick_\\source\\repos\\KuCoinLend\\";
        JObject jsonLendPrecision = new();
        bool lendMultiples = false;
        //       long compareTime = 0;
        public string KCresponse;
        double minimumLendIncrement = 0.00001;
        double currentLowestLendRate = 0;
        double availableBalance = 0;
        string lendPrecision = "0.1";
        bool prodRun = true;
        bool dumpJsonResponses = true;


        public Lend(int hoursToLive, string ticker)

        {
            this.hoursToLive = hoursToLive;
            this.ticker = ticker;
            ReportIt($"Prod run = {this.prodRun}--------starting instance for " + this.ticker + " --------");
            this.getCurrencyPrecision();
            if (this.GetLendingRates())
            {
                this.processOpenOrders();
                this.GetAvailableBalance();
                this.LendCoin();
            }


        }

        public void getCurrencyPrecision()
        {
            string jsonPrecision = "min_lend_amount.json";
            if (File.Exists(baseWorkDir + jsonPrecision))
            {
                jsonLendPrecision = JObject.Parse(File.ReadAllText(baseWorkDir + jsonPrecision));
                string? prec = (string)jsonLendPrecision[this.ticker];
                if (prec != null)
                {
                    if (prec.Contains("*"))
                    {
                        lendMultiples = true;
                        prec = prec.Replace("*", "");

                    }
                    this.lendPrecision = prec;

                }
                else
                {
                    this.lendPrecision = "0.1";
                }
            }
        }
        public void processOpenOrders()
        {
            string point = @"/api/v1/margin/lend/active?currency=" + this.ticker + "&currentPage=1&pageSize=50";
            if (this.prodRun)
            {
                KCresponse = KuCoinCall.get_results(point, "GET");

                if (this.dumpJsonResponses)
                {
                    File.WriteAllText(@"C:\Users\nick_\source\repos\KuCoinLend\" + "ETH.json", KCresponse);
                }
            }
            else
            {
                KCresponse = File.ReadAllText(@"C:\Users\nick_\source\repos\KuCoinLend\" + "ETH.json");
            }

            JObject jsonObj = JObject.Parse(KCresponse);

            JArray array = (JArray)jsonObj["data"]["items"];

            //work out the time
            long systemTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long timeOffset = 60 * 60 * this.hoursToLive;

            long compareTime = (systemTime - timeOffset * 1000); // * 1000;

            foreach (var j in array)
            {
                if ((long)j["createdAt"] < compareTime)   //check if too old
                {

                    if (this.currentLowestLendRate != (double)j["dailyIntRate"])   //not at current lowest rate
                    {//delete old orders
                        if (this.prodRun)
                        {
                            point = @"/api/v1/margin/lend/" + j["orderId"];
                            KCresponse = KuCoinCall.get_results(point, "DELETE");
                            jsonObj = JObject.Parse(KCresponse);
                            if ((string)jsonObj["code"] == "200000")
                            {
                                ReportIt($"order {j["orderId"]} deleted");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"would be deleting orderId {j["orderId"]}");
                        }
                    }

                    else
                    {
                        ReportIt($"order {j["orderId"]} not deleted already at lowest rate {this.currentLowestLendRate.ToString("#0.00000")}");
                    }
                }
            }
        }
        public void LendCoin()
        {
            if (Convert.ToDouble(this.availableBalance) < Convert.ToDouble(this.lendPrecision))
            {
                ReportIt($"not enough {this.ticker},  {this.availableBalance} available, minimum required {this.lendPrecision}");

            }
            else
            {
                ReportIt($"{this.availableBalance} {this.ticker} available, precision {this.lendPrecision}");

                ArrayList splitList = splitAmount();
                int nbrOfLendOrders = Convert.ToInt32(splitList[0]);

                for (int i = 0; i < nbrOfLendOrders; i++)
                {
                    double lendRate = this.currentLowestLendRate + (i * this.minimumLendIncrement);
                    string rate = lendRate.ToString("#0.00000");
                    //.ToString("#0.00000");
                    JObject jsonLend = new JObject(
                        new JProperty("currency", this.ticker),
                        new JProperty("size", splitList[1]),
                        new JProperty("dailyIntRate", rate),
                        new JProperty("term", 7));

                    KCresponse = KuCoinCall.get_results("/api/v1/margin/lend", "POST", jsonLend.ToString());

                    JObject jsonObj = JObject.Parse(KCresponse);
                    var placedOrderId = jsonObj["data"]["orderId"];
                    ReportIt($"order number {placedOrderId.ToString()} placed {this.ticker} {splitList[1]}");

                }
            }
        }

        public ArrayList splitAmount()
        {
            double temp = Convert.ToDouble(this.lendPrecision);
            ArrayList splitList = new ArrayList();

            for (int i = 3; i > 0; i--)
            {
                double amt = this.availableBalance / i;
                if (amt >= temp)
                {
                    splitList.Add(i);
                    splitList.Add(truncateAmt(amt));

                    return splitList;
                }
            }
            return splitList;
        }

        private string truncateAmt(double amt)
        {
            string truncatedAmt;
            int index1 = this.lendPrecision.IndexOf(".");
            if (index1 == -1)
            {
                truncatedAmt = Convert.ToString(Math.Floor(amt));

            }
            else
            {
                string[] splitAmount = this.lendPrecision.Split(".");
                int nbrOfDecimalPlacesNeeded = splitAmount[1].Length;
                string temp = Convert.ToString(amt);
                int splitIndex = temp.IndexOf(".");
                truncatedAmt = temp.Substring(0, splitIndex + nbrOfDecimalPlacesNeeded + 1);
            }
            //this.lendMultiples = true;
            if (this.lendMultiples)
            {
                //string[] splitAmount = this.lendPrecision.Split(".");
                //int nbrOfDecimalPlacesNeeded = splitAmount[1].Length;
                double amount = Convert.ToDouble(truncatedAmt);
                double dropThisMuch = amount % Convert.ToDouble(this.lendPrecision);
                truncatedAmt = Convert.ToString((double)(amount - dropThisMuch));
                //int splitIndex = truncatedAmt.IndexOf(".");
                //truncatedAmt = truncatedAmt.Substring(0, splitIndex + nbrOfDecimalPlacesNeeded + 1);

            }

            return truncatedAmt;
        }

        private void GetAvailableBalance()
        {
            this.availableBalance = 0;
            string point = @"/api/v1/accounts?currency=" + this.ticker;

            string KCresponse = KuCoinCall.get_results(point, "GET");

            JObject jsonObj = JObject.Parse(KCresponse);
            if (this.dumpJsonResponses)
            {
                File.WriteAllText(@"C:\Users\nick_\source\repos\KuCoinLend\" + this.ticker + "_accoutdata.json", jsonObj.ToString());
            }

            JArray array = (JArray)jsonObj["data"];
            foreach (var j in array)
            {
                if ((string)j["type"] == "main")
                {
                    this.availableBalance = (double)j["available"];
                    ReportIt($"Available balance {j["available"]}{this.ticker}");
                }
            }
        }

        private bool GetLendingRates()
        {
            if (this.prodRun)
            {
                string point = @"/api/v1/margin/market?currency=" + this.ticker + "&term=7";
                KCresponse = KuCoinCall.get_results(point, "GET");
                //JObject jsonObj = JObject.Parse(KCresponse);
            }
            else
            {
                KCresponse = File.ReadAllText(@"C:\Users\nick_\source\repos\KuCoinLend\" + this.ticker + "_lendingratesResponse.json");
            }

            JObject jsonObj = JObject.Parse(KCresponse);
            if (this.prodRun)
            {
                if (this.dumpJsonResponses)
                {
                    File.WriteAllText(@"C:\Users\nick_\source\repos\KuCoinLend\" + this.ticker + "_lendingratesResponse.json", jsonObj.ToString());
                }
            }

            JToken jtok = jsonObj["data"];

            if (jtok.Count<object>() == 0)
            {
                ReportIt($"{this.ticker} cannot be lent out, no lending data available, added to unlendable.csv");
                File.AppendAllText(baseWorkDir + "unlendable.csv", this.ticker + "\n");
                return false;
            }

            this.currentLowestLendRate = (double)jsonObj["data"][0]["dailyIntRate"];
            return true;
        }
        public void ReportIt(string msg)
        {
            //baseWorkDir
            var currTime = DateTime.Now;
            msg = "[" + currTime.ToString() + "]" + " " + msg;
            File.AppendAllText(baseWorkDir + "trans_log.txt", msg + "\n");

        }

    }
}
