using System.Security.Cryptography;
using System.Text;

namespace CryptoExchangeClient.Helpers
{
    public static class PasswordHelper
    {
        public static string ComputeSha256(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] hash;

            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(bytes);
            }

            StringBuilder sb = new StringBuilder();

            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}