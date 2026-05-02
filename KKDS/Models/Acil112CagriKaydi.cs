using System;
using System.Text.Json.Serialization;

namespace KKDS.Models
{
    /// <summary>112 acil yardım çağrısı: mahkum, vardiyalar ve şikâyet bilgisi.</summary>
    public class Acil112CagriKaydi : BaseKayit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("mahkum_id")]
        public int MahkumId { get; set; }

        /// <summary>Kayıt anındaki ad soyad (listelerde hızlı gösterim).</summary>
        [JsonPropertyName("mahkum_ad_soyad")]
        public string MahkumAdSoyad { get; set; } = string.Empty;

        [JsonPropertyName("cagri_zamani")]
        public DateTime CagriZamani { get; set; } = DateTime.Now;

        /// <summary>Olayın veya başvurunun ilişkilendirildiği vardiya dilimi.</summary>
        [JsonPropertyName("vardiya")]
        public string Vardiya { get; set; } = string.Empty;

        /// <summary>112 acil hattını arayan nöbet / vardiya.</summary>
        [JsonPropertyName("cagiran_vardiya")]
        public string CagiranVardiya { get; set; } = string.Empty;

        [JsonPropertyName("sikayet")]
        public string Sikayet { get; set; } = string.Empty;

        [JsonPropertyName("notlar")]
        public string Notlar { get; set; } = string.Empty;

        public Acil112CagriKaydi()
        {
            KayitTuru = "acil112";
        }
    }

    public static class NobetVardiyalari
    {
        public static readonly string[] Tumunu =
        {
            "1. Vardiya",
            "2. Vardiya",
            "3. Vardiya",
            "Diğer / nöbet değişimi"
        };
    }
}
