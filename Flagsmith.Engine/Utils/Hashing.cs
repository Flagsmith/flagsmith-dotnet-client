using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
namespace FlagsmithEngine.Utils
{
    public class Hashing
    {
        public virtual float GetHashedPercentageForObjectIds(List<string> objectIds, int iteration = 1)
        {
            var toHash = String.Join(",", repeatIdsList(objectIds, iteration));
            var hashedValueAsInt = CreateMD5AsInt(toHash);
            var value = ((float)(hashedValueAsInt % 9999) / 9998) * 100;
            return value == 100 ? GetHashedPercentageForObjectIds(objectIds, ++iteration) : value;
        }
        public List<string> repeatIdsList(List<string> objectIds, int iteration)
        {
            var list = new List<string>();
            foreach (var _ in Enumerable.Range(1, iteration))
            {
                list.AddRange(objectIds);
            }
            return list;
        }
        public BigInteger CreateMD5AsInt(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = ComputeHash(input);
                var sb = HashBytesToString(hashBytes);
                //return a negative number if the first digit is between 8-F.so prepend a 0 to the string.
                return BigInteger.Parse("0" + sb.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier);
            }
        }
        public virtual byte[] ComputeHash(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }
        public virtual string HashBytesToString(byte[] hashBytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var hashByte in hashBytes)
            {
                sb.Append(hashByte.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
