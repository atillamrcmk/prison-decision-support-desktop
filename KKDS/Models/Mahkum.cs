using System;
using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public class Mahkum : BaseKayit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("ad_soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [JsonPropertyName("blok")]
        public string Blok { get; set; } = string.Empty;

        [JsonPropertyName("kogus")]
        public string Kogus { get; set; } = string.Empty;

        [JsonPropertyName("kuruma_giris_tarihi")]
        public DateTime KurumaGirisTarihi { get; set; } = DateTime.Today;

        [JsonPropertyName("durum")]
        public string Durum { get; set; } = "aktif";

        [JsonPropertyName("not")]
        public string Not { get; set; } = string.Empty;

        public Mahkum()
        {
            KayitTuru = "mahkum";
        }
    }

    public static class MahkumDurumlari
    {
        public static readonly string[] Tumunu = { "aktif", "yakin_izlem", "tahliye_edildi", "nakil" };
    }

    public static class Bloklar
    {
        public static readonly string[] Tumunu = { "A-Blok", "B-Blok", "C-Blok", "D-Blok" };
    }
}
