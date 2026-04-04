using System;
using System.Collections.Generic;

namespace KKDS.Services
{
    public static class GirisDenemesiYoneticisi
    {
        private const int UyariEsigi = 3;
        private const int MaksDeneme = 8;
        private static readonly Dictionary<string, (int Sayi, DateTime Son)> _denemeler = new(StringComparer.OrdinalIgnoreCase);

        public static void Basarili(string kullaniciAdi)
        {
            if (string.IsNullOrEmpty(kullaniciAdi)) return;
            _denemeler.Remove(kullaniciAdi);
        }

        public static int BasarisizKaydet(string kullaniciAdi)
        {
            if (string.IsNullOrEmpty(kullaniciAdi)) kullaniciAdi = "?";
            var simdi = DateTime.UtcNow;
            if (!_denemeler.TryGetValue(kullaniciAdi, out var v))
            {
                _denemeler[kullaniciAdi] = (1, simdi);
                return 1;
            }
            if ((simdi - v.Son).TotalMinutes > 15)
                v.Sayi = 0;
            v.Sayi++;
            v.Son = simdi;
            _denemeler[kullaniciAdi] = v;
            return v.Sayi;
        }

        public static bool UyariGosterilsinMi(string kullaniciAdi)
        {
            if (string.IsNullOrEmpty(kullaniciAdi)) return false;
            return _denemeler.TryGetValue(kullaniciAdi, out var v) && v.Sayi >= UyariEsigi && v.Sayi < MaksDeneme;
        }

        public static string UyariMetni(int ardisikHata)
        {
            if (ardisikHata >= MaksDeneme)
                return "Çok sayıda hatalı giriş denemesi yapıldı. Bir süre sonra tekrar deneyin veya yöneticiye başvurun.";
            if (ardisikHata >= UyariEsigi)
                return $"Son {ardisikHata} denemede giriş başarısız. Bilgilerinizi kontrol edin.";
            return "";
        }
    }
}
