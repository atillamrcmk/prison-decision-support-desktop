using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public class KurulOnay
    {
        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;

        [JsonPropertyName("kullanici")]
        public string Kullanici { get; set; } = string.Empty;

        [JsonPropertyName("kullanici_ad")]
        public string KullaniciAd { get; set; } = string.Empty;

        [JsonPropertyName("durum")]
        public string Durum { get; set; } = OnayDurumlari.Beklemede;

        [JsonPropertyName("yorum")]
        public string Yorum { get; set; } = string.Empty;

        [JsonPropertyName("tarih")]
        public DateTime? Tarih { get; set; }
    }

    public class KurulKarari : BaseKayit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("mahkum_id")]
        public int MahkumId { get; set; }

        [JsonPropertyName("karar_tarihi")]
        public DateTime KararTarihi { get; set; } = DateTime.Today;

        [JsonPropertyName("karar_turu")]
        public string KararTuru { get; set; } = string.Empty;

        [JsonPropertyName("sure_gun")]
        public int SureGun { get; set; }

        [JsonPropertyName("kisa_gerekce")]
        public string KisaGerekce { get; set; } = string.Empty;

        [JsonPropertyName("detayli_gerekce")]
        public string DetayliGerekce { get; set; } = string.Empty;

        [JsonPropertyName("gozden_gecirme_tarihi")]
        public DateTime? GozdenGecirmeTarihi { get; set; }

        [JsonPropertyName("onay_durumu")]
        public string OnayDurumu { get; set; } = OnayDurumlari.Beklemede;

        [JsonPropertyName("onaylar")]
        public List<KurulOnay> Onaylar { get; set; } = new();

        [JsonPropertyName("kesinlesme_tarihi")]
        public DateTime? KesinlesmeTarihi { get; set; }

        public KurulKarari()
        {
            KayitTuru = "kurul";
        }

        public void OnayBilgisiOlustur()
        {
            if (Onaylar.Count > 0) return;
            Onaylar = new List<KurulOnay>
            {
                new() { Rol = Roller.Psikolog, Durum = OnayDurumlari.Beklemede },
                new() { Rol = Roller.Revir, Durum = OnayDurumlari.Beklemede },
                new() { Rol = Roller.Disiplin, Durum = OnayDurumlari.Beklemede }
            };
        }

        public void OnayDurumuGuncelle()
        {
            if (Onaylar.Count == 0) return;

            if (Onaylar.Any(o => o.Durum == OnayDurumlari.Reddedildi))
            {
                OnayDurumu = OnayDurumlari.Reddedildi;
                return;
            }

            if (Onaylar.All(o => o.Durum == OnayDurumlari.Onaylandi))
            {
                OnayDurumu = OnayDurumlari.Onaylandi;
                KesinlesmeTarihi = DateTime.Now;
                return;
            }

            OnayDurumu = OnayDurumlari.Beklemede;
        }

        public bool RolOnayVermisMi(string rol) =>
            Onaylar.Any(o => o.Rol == rol && o.Durum != OnayDurumlari.Beklemede);

        public int OnaylayanSayisi => Onaylar.Count(o => o.Durum == OnayDurumlari.Onaylandi);
        public int ToplamOnayGerekli => Onaylar.Count;
    }

    public static class OnayDurumlari
    {
        public const string Beklemede = "beklemede";
        public const string Onaylandi = "onaylandi";
        public const string Reddedildi = "reddedildi";

        public static string Goster(string d) => d switch
        {
            Beklemede => "Beklemede",
            Onaylandi => "Onaylandı",
            Reddedildi => "Reddedildi",
            _ => d
        };

        public static string Renk(string d) => d switch
        {
            Onaylandi => "#28A745",
            Reddedildi => "#DC3545",
            _ => "#FFC107"
        };
    }

    public static class KararTurleri
    {
        public const string HucreCezasi = "Hücre cezası";
        public const string Uyari = "Uyarı";
        public const string GozlemAltinaAlma = "Gözlem altına alma";
        public const string IyiHalIndirimi = "İyi hal indirimi";
        public const string PsikologYonlendirme = "Psikolog yönlendirme";
        public const string BlokDegisikligi = "Blok değişikliği";
        public const string ZiyaretKisitlamasi = "Ziyaret kısıtlaması";

        public static readonly string[] Tumunu = {
            HucreCezasi, Uyari, GozlemAltinaAlma, IyiHalIndirimi,
            PsikologYonlendirme, BlokDegisikligi, ZiyaretKisitlamasi
        };
    }
}
