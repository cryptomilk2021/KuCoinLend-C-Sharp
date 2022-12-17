namespace KuCoinLend
{
    public static class GetOSVariables
    {
        public static readonly string? apiKey = Environment.GetEnvironmentVariable("APIKEY", EnvironmentVariableTarget.User);
        public static readonly string? apiSecret = Environment.GetEnvironmentVariable("APISECRET", EnvironmentVariableTarget.User);
        public static readonly string? apiPassPhrase = Environment.GetEnvironmentVariable("APIPASSPHRASE", EnvironmentVariableTarget.User);
    }
}
