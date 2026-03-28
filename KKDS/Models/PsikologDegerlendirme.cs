using System;
using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public class PsikologDegerlendirme : BaseKayit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("mahkum_id")]
        public int MahkumId { get; set; }

        [JsonPropertyName("degerlendirme_tarihi")]
        public DateTime DegerlendirmeTarihi { get; set; } = DateTime.Today;

        [JsonPropertyName("ruh_hali")]
        public string RuhHali { get; set; } = string.Empty;

        [JsonPropertyName("agresyon_duzeyi")]
        public int AgresyonDuzeyi { get; set; } = 1;

        [JsonPropertyName("kendine_zarar_riski")]
        public int KendineZararRiski { get; set; } = 1;

        [JsonPropertyName("isbirligi")]
        public int Isbirligi { get; set; } = 3;

        [JsonPropertyName("onceki_duruma_gore")]
        public string OncekiDurumaGore { get; set; } = "Aynı";

        [JsonPropertyName("aciklama")]
        public string Aciklama { get; set; } = string.Empty;

        public PsikologDegerlendirme()
        {
            KayitTuru = "psikolog";
        }
    }

    public static class RuhHalleri
    {
        public static readonly string[] Tumunu = {
            "Sakin", "Dengeli", "Gergin", "Endişeli", "Kayıtsız", "Depresif", "Agresif"
        };
    }

    public static class OncekiDurumlar
    {
        public static readonly string[] Tumunu = { "İyileşme", "Aynı", "Kötüleşme" };
    }
}
