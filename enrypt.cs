using System.Security.Cryptography;
using System.Text;

namespace KuCoinLend
{
    class encrypt
    {
        public static string HmacSha256(string message, string secret)
        {
            var encoding = new ASCIIEncoding();
            var msgBytes = encoding.GetBytes(message);
            var secretBytes = encoding.GetBytes(secret);
            var hmac = new HMACSHA256(secretBytes);

            var hash = hmac.ComputeHash(msgBytes);

            return Convert.ToBase64String(hash);
        }

    }
}
