namespace KKDS.Models
{
    /// <summary>Kurum çapında özet göstergeler (gösterge paneliyle uyumlu veri kaynağı).</summary>
    public sealed class KurumGenelAnalizOzet
    {
        public int ToplamMahkum { get; set; }
        public int KogusSayisi { get; set; }
        public int BlokSayisi { get; set; }
        public int Son30GunOlay { get; set; }
        public int Son30GunKarar { get; set; }
        public int YakinIzlemSayisi { get; set; }
        public double OrtalamaRisk { get; set; }
        public string EnYuksekRiskKod { get; set; } = "-";
        public double EnYuksekRiskDeger { get; set; }
        public string EnCokOlayKogusu { get; set; } = "-";
        public int EnCokOlayKogusuAdet { get; set; }
        public string EnYuksekOrtRiskKogusu { get; set; } = "-";
        public double EnYuksekOrtRiskDeger { get; set; }
        public string OzetMetin { get; set; } = "";
    }

    /// <summary>Tek bir koğuş (blok + koğuş no) için toplu analiz satırı.</summary>
    public sealed class KogusAnalizSatir
    {
        public string Blok { get; set; } = "";
        public string Kogus { get; set; } = "";
        public int MahkumSayisi { get; set; }
        public int Son30GunOlay { get; set; }
        public int Son30GunKarar { get; set; }
        /// <summary>Son 30 günde mahkum başına düşen olay (yoğunluk göstergesi).</summary>
        public double OlayBasiMahkum { get; set; }
        public double OrtalamaRisk { get; set; }
        public double EnYuksekRisk { get; set; }
        public string EnRiskliMahkumKodu { get; set; } = "";
        public int EnRiskliMahkumId { get; set; }
        public int YakinIzlemSayisi { get; set; }
    }

    /// <summary>Blok bazlı özet (kurum görünümü).</summary>
    public sealed class BlokAnalizSatir
    {
        public string Blok { get; set; } = "";
        public int MahkumSayisi { get; set; }
        public int KogusSayisi { get; set; }
        public int Son30GunOlay { get; set; }
        public int Son30GunKarar { get; set; }
        public double OrtalamaRisk { get; set; }
    }
}
