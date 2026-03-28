using System;
using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public class Olay : BaseKayit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("mahkum_id")]
        public int MahkumId { get; set; }

        [JsonPropertyName("olay_tarihi")]
        public DateTime OlayTarihi { get; set; } = DateTime.Today;

        [JsonPropertyName("olay_turu")]
        public string OlayTuru { get; set; } = string.Empty;

        [JsonPropertyName("siddet")]
        public int Siddet { get; set; } = 1;

        [JsonPropertyName("hedef")]
        public string Hedef { get; set; } = string.Empty;

        [JsonPropertyName("zaman_dilimi")]
        public string ZamanDilimi { get; set; } = string.Empty;

        [JsonPropertyName("aciklama")]
        public string Aciklama { get; set; } = string.Empty;

        public Olay()
        {
            KayitTuru = "disiplin";
        }
    }

    public static class OlayTurleri
    {
        public const string Kavga = "Kavga";
        public const string FizikselSaldiri = "Fiziksel saldırı";
        public const string SozelTehdit = "Sözel tehdit";
        public const string HuzursuzlukCikarma = "Huzursuzluk çıkarma";
        public const string Itaatsizlik = "İtaatsizlik";
        public const string KendineZararVerme = "Kendine zarar verme";
        public const string EsyayaZararVerme = "Eşyaya zarar verme";

        public static readonly string[] Tumunu = {
            Kavga, FizikselSaldiri, SozelTehdit, HuzursuzlukCikarma,
            Itaatsizlik, KendineZararVerme, EsyayaZararVerme
        };

        public static readonly string[] FizikselOlaylar = {
            Kavga, FizikselSaldiri, KendineZararVerme
        };

        public static readonly string[] SozelOlaylar = {
            SozelTehdit, HuzursuzlukCikarma, Itaatsizlik
        };
    }

    public static class Hedefler
    {
        public static readonly string[] Tumunu = { "Gardiyan", "Diğer mahkum", "Kendisi", "Eşya" };
    }

    public static class ZamanDilimleri
    {
        public static readonly string[] Tumunu = { "Sabah", "Öğle", "Akşam", "Gece" };
    }
}
