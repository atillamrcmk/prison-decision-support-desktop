using System;
using System.Security.Cryptography;
using System.Text;

namespace KKDS.Services
{
    /// <summary>SHA-256 + kullanıcı bazlı tuz ile şifre özeti (pilot için; üretimde Argon2 tercih edilir).</summary>
    public static class SifreServisi
    {
        private const int TuzBayt = 16;

        public static string TuzOlustur()
        {
            var bytes = RandomNumberGenerator.GetBytes(TuzBayt);
            return Convert.ToBase64String(bytes);
        }

        public static string HashOlustur(string duzMetin, string tuzBase64)
        {
            if (string.IsNullOrEmpty(tuzBase64)) tuzBase64 = TuzOlustur();
            var tuz = Convert.FromBase64String(tuzBase64);
            var sifreBytes = Encoding.UTF8.GetBytes(duzMetin ?? "");
            var birlesik = new byte[tuz.Length + sifreBytes.Length];
            Buffer.BlockCopy(tuz, 0, birlesik, 0, tuz.Length);
            Buffer.BlockCopy(sifreBytes, 0, birlesik, tuz.Length, sifreBytes.Length);
            var hash = SHA256.HashData(birlesik);
            return Convert.ToBase64String(hash);
        }

        public static bool Dogrula(string duzMetin, string? hashBase64, string? tuzBase64)
        {
            if (string.IsNullOrEmpty(hashBase64) || string.IsNullOrEmpty(tuzBase64)) return false;
            try
            {
                var hesaplanan = HashOlustur(duzMetin, tuzBase64);
                return CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(hesaplanan),
                    Convert.FromBase64String(hashBase64));
            }
            catch
            {
                return false;
            }
        }
    }
}
