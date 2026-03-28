using System;
using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public class RevirKaydi : BaseKayit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("mahkum_id")]
        public int MahkumId { get; set; }

        [JsonPropertyName("kayit_tarihi")]
        public DateTime KayitTarihi { get; set; } = DateTime.Today;

        [JsonPropertyName("uyku_durumu")]
        public string UykuDurumu { get; set; } = "Normal";

        [JsonPropertyName("ilac_uyumu")]
        public string IlacUyumu { get; set; } = "Tam";

        [JsonPropertyName("stres_seviyesi")]
        public int StresSeviyesi { get; set; } = 1;

        [JsonPropertyName("davranis_etkisi")]
        public string DavranisEtkisi { get; set; } = "Nötr";

        [JsonPropertyName("aciklama")]
        public string Aciklama { get; set; } = string.Empty;

        public RevirKaydi()
        {
            KayitTuru = "revir";
        }
    }

    public static class UykuDurumlari
    {
        public static readonly string[] Tumunu = { "Normal", "Bozuk", "Uykusuzluk" };
    }

    public static class IlacUyumlari
    {
        public static readonly string[] Tumunu = { "Tam", "Kısmi", "Reddediyor" };
    }

    public static class DavranisEtkileri
    {
        public static readonly string[] Tumunu = { "Pozitif", "Nötr", "Negatif" };
    }
}
