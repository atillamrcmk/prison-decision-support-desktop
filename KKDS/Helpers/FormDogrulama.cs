using System;

namespace KKDS.Helpers
{
    public static class FormDogrulama
    {
        public const int AciklamaMaxUzunluk = 4000;

        public static bool BosMu(string? s) => string.IsNullOrWhiteSpace(s);

        public static string? MahkumKoduKontrol(string? kod)
        {
            if (BosMu(kod)) return "Mahkum kodu zorunludur.";
            if (kod!.Trim().Length < 2) return "Mahkum kodu en az 2 karakter olmalıdır.";
            return null;
        }

        public static string? TarihKontrol(DateTime t, string alanAdi = "Tarih")
        {
            if (t.Date > DateTime.Today)
                return $"{alanAdi} gelecek bir tarih olamaz.";
            return null;
        }

        public static string? ComboZorunlu(string? secilen, string alanAdi)
        {
            if (BosMu(secilen)) return $"{alanAdi} seçilmelidir.";
            return null;
        }

        public static string? AciklamaUzunluk(string? aciklama)
        {
            if (aciklama != null && aciklama.Length > AciklamaMaxUzunluk)
                return $"Açıklama en fazla {AciklamaMaxUzunluk} karakter olabilir.";
            return null;
        }

        public static string Birlestir(params string?[] hatalar)
        {
            foreach (var h in hatalar)
                if (!string.IsNullOrEmpty(h)) return h;
            return "";
        }
    }
}
