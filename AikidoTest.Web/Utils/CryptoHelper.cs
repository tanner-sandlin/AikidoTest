using System;
using System.Security.Cryptography;
using System.Text;

namespace AikidoTest.Web.Utils
{
    public static class CryptoHelper
    {
        // CWE-798: hardcoded symmetric key, in addition to the one duplicated in Web.config
        private static readonly byte[] Key = Encoding.ASCII.GetBytes("8Bytes1!");
        private static readonly byte[] Iv = Encoding.ASCII.GetBytes("12345678");

        // CWE-327: MD5 used for password hashing (fast, unsalted, broken for this purpose)
        public static string HashPassword(string password)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // CWE-327 / CWE-326: DES is a broken cipher with a 56-bit effective key,
        // used here to "protect" stored card numbers.
        public static string EncryptCardNumber(string plainText)
        {
            using (var des = DES.Create())
            {
                des.Key = Key;
                des.IV = Iv;
                using (var encryptor = des.CreateEncryptor())
                {
                    var input = Encoding.UTF8.GetBytes(plainText);
                    var output = encryptor.TransformFinalBlock(input, 0, input.Length);
                    return Convert.ToBase64String(output);
                }
            }
        }
    }
}
