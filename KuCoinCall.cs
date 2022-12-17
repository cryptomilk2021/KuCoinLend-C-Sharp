using Newtonsoft.Json.Linq;
using RestSharp;

namespace KuCoinLend
{
    public class KuCoinCall
    {

        //        public static string get_results(string point, string callType)
        public static string get_results(params string[] passedArgs)

        {
            if (passedArgs.Length < 2 || passedArgs.Length > 3)
            {
                Console.WriteLine("incorrect amount of vars passed to get_results");
            }

            string point = passedArgs[0];
            string callType = passedArgs[1];
            JObject jsonData = new JObject();
            long systemTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string stringToSign = systemTime.ToString() + callType + point;
            string passedJsonData = "";
            if (passedArgs.Length == 3)
            {
                passedJsonData = passedArgs[2];
                stringToSign = systemTime.ToString() + callType + point + passedJsonData;
            }

            string? apiKey = GetOSVariables.apiKey;
            string? apiSecret = GetOSVariables.apiSecret;
            string? apiPassPhrase = GetOSVariables.apiPassPhrase;
            string Uri = "https://api.kucoin.com";

            string signature = encrypt.HmacSha256(stringToSign, apiSecret);

            string passPhrase = encrypt.HmacSha256(apiPassPhrase, apiSecret);

            var client = new RestClient(Uri);
            var request = new RestRequest(point, Method.Get);

            if (callType == "GET")
            {
                request = new RestRequest(point, Method.Get);
            }
            else
            {
                if (callType == "DELETE")
                {
                    request = new RestRequest(point, Method.Delete);
                }
                else
                {
                    request = new RestRequest(point, Method.Post);
                }
            }

            request.AddHeader("KC-API-SIGN", signature);
            request.AddHeader("KC-API-TIMESTAMP", systemTime.ToString());
            request.AddHeader("KC-API-KEY", apiKey);
            request.AddHeader("KC-API-PASSPHRASE", passPhrase);
            request.AddHeader("KC-API-KEY-VERSION", "2");
            request.AddHeader("Content-Type", "application/json; charset=utf-8");
            if (callType == "POST")
            {
                request.AddParameter("application/json", passedJsonData, ParameterType.RequestBody);
            }

            var response = client.Execute(request);
            JObject jsonObj = JObject.Parse(response.Content);

            return jsonObj.ToString();

        }
    }
}
