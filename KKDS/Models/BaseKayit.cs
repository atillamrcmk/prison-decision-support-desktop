using System;
using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public abstract class BaseKayit
    {
        [JsonPropertyName("kayit_id")]
        public string KayitId { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpper();

        [JsonPropertyName("kayit_turu")]
        public string KayitTuru { get; set; } = string.Empty;

        [JsonPropertyName("mahkum_kodu")]
        public string MahkumKodu { get; set; } = string.Empty;

        [JsonPropertyName("giren_kullanici")]
        public string GirenKullanici { get; set; } = string.Empty;

        [JsonPropertyName("giren_rol")]
        public string GirenRol { get; set; } = string.Empty;

        [JsonPropertyName("olusturan_kullanici")]
        public string OlusturanKullanici { get; set; } = string.Empty;

        [JsonPropertyName("guncelleyen_kullanici")]
        public string GuncelleyenKullanici { get; set; } = string.Empty;

        [JsonPropertyName("olusturma_zamani")]
        public DateTime OlusturmaZamani { get; set; } = DateTime.Now;

        [JsonPropertyName("guncelleme_zamani")]
        public DateTime GuncellemeZamani { get; set; } = DateTime.Now;

        [JsonPropertyName("aktif_mi")]
        public bool AktifMi { get; set; } = true;

        [JsonPropertyName("is_demo_data")]
        public bool IsDemoData { get; set; }
    }
}
