using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public class Kullanici
    {
        [JsonPropertyName("kullanici_adi")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [JsonPropertyName("sifre")]
        public string Sifre { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;

        [JsonPropertyName("ad_soyad")]
        public string AdSoyad { get; set; } = string.Empty;
    }

    public static class Roller
    {
        public const string Psikolog = "psikolog";
        public const string Revir = "revir";
        public const string Disiplin = "disiplin";
        public const string Yonetici = "yonetici";

        public static readonly string[] Tumunu = { Psikolog, Revir, Disiplin, Yonetici };

        public static string RolAdi(string rol) => rol switch
        {
            Psikolog => "Psikolog",
            Revir => "Revir",
            Disiplin => "Disiplin",
            Yonetici => "Yönetici",
            _ => rol
        };
    }
}
