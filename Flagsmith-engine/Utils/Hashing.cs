using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace Flagsmith_engine.Utils
{
    internal static class Hashing
    {
        public static float GetHashedPercentageForObjectIds(List<string> objectIds, int iteration = 1)
        {
            var toHash = String.Join(",", repeatIdsList(objectIds, iteration));
            var hashedValueAsInt = BitConverter.ToInt64(CreateMD5(toHash),0);
            var SS = hashedValueAsInt % 9999;
            float SSS = SS / 9998;
            var value = ((hashedValueAsInt % 9999) / 9998) * 100;
            return value == 100 ? GetHashedPercentageForObjectIds(objectIds, ++iteration) : value;
        }
        private static List<string> repeatIdsList(List<string> objectIds, int iteration)
        {
            var list = new List<string>();
            foreach (var _ in Enumerable.Range(1, iteration))
            {
                list.AddRange(objectIds);
            }
            return list;
        }
        public static byte[] CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return hashBytes;
            }
        }
    }
}
