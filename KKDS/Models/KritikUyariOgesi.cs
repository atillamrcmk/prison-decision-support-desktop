namespace KKDS.Models
{
    /// <summary>Dashboard kritik uyarı satırı.</summary>
    public class KritikUyariOgesi
    {
        public int MahkumId { get; set; }
        public string MahkumKodu { get; set; } = "";
        /// <summary>Kısa açıklama (mahkum kodu hariç).</summary>
        public string Mesaj { get; set; } = "";
        /// <summary>kritik (kırmızı) veya uyari (sarı).</summary>
        public string Seviye { get; set; } = "uyari";
        public string Tip { get; set; } = "";

        public string SatirMetni => string.IsNullOrEmpty(MahkumKodu) ? Mesaj : $"{MahkumKodu} → {Mesaj}";
    }
}
