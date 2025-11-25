using System;
using System.Security.Cryptography;
using System.Text;

namespace Flagsmith
{
    internal static class Utils
    {
        /// <summary>
        /// Computes SHA256 hash of the input text and returns it as a lowercase hex string.
        /// </summary>
        internal static string GetHashString(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
