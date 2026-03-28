using System;
using System.Linq;
using KKDS.Models;

namespace KKDS.Services
{
    public static class TestVeriOlusturucu
    {
        private static readonly Random _rng = new(42);

        public static void Olustur()
        {
            var veri = VeriDepolamaServisi.Instance;
            if (veri.TestVerisiVarMi()) return;

            var profiller = new (string Kod, string Ad, int Risk, string Blok, string Durum)[]
            {
                ("M-2025001", "Ahmet Yılmaz",    5, "A-Blok", "yakin_izlem"),
                ("M-2025002", "Mehmet Kara",      4, "A-Blok", "aktif"),
                ("M-2025003", "Ali Demir",        3, "B-Blok", "aktif"),
                ("M-2025004", "Hasan Çelik",      3, "B-Blok", "aktif"),
                ("M-2025005", "Murat Şahin",      2, "C-Blok", "aktif"),
                ("M-2025006", "Emre Özkan",       2, "C-Blok", "aktif"),
                ("M-2025007", "Kemal Arslan",     1, "D-Blok", "aktif"),
                ("M-2025008", "Serkan Aydın",     1, "D-Blok", "aktif"),
                ("M-2025009", "Cem Koç",          4, "A-Blok", "yakin_izlem"),
                ("M-2025010", "Burak Erdoğan",    3, "B-Blok", "aktif"),
            };

            for (int i = 0; i < profiller.Length; i++)
            {
                var p = profiller[i];
                var m = new Mahkum
                {
                    Id = i + 1,
                    MahkumKodu = p.Kod,
                    AdSoyad = p.Ad,
                    Blok = p.Blok,
                    Kogus = $"{_rng.Next(1, 8)}",
                    KurumaGirisTarihi = DateTime.Today.AddDays(-_rng.Next(90, 365)),
                    Durum = p.Durum,
                    GirenKullanici = "sistem",
                    GirenRol = "sistem"
                };
                veri.MahkumKaydet(m);
                OlaylarOlustur(m.Id, p.Risk);
                PsikologOlustur(m.Id, p.Risk);
                RevirOlustur(m.Id, p.Risk);
                KurulOlustur(m.Id, p.Risk);
            }

            LogServisi.Instance.Bilgi("sistem", "sistem", "Test verisi oluşturuldu (10 mahkum)");
        }

        private static void OlaylarOlustur(int mid, int risk)
        {
            int sayi = risk switch { 5 => _rng.Next(18, 25), 4 => _rng.Next(12, 18), 3 => _rng.Next(6, 12), 2 => _rng.Next(3, 6), _ => _rng.Next(0, 3) };
            bool sozdenFiziksele = mid == 1;
            bool hizliKotu = mid == 9;

            for (int i = 0; i < sayi; i++)
            {
                int gun; string tur; int siddet;
                if (hizliKotu && i >= sayi - 6)
                { gun = _rng.Next(0, 10); tur = Sec(OlayTurleri.FizikselOlaylar); siddet = _rng.Next(3, 6); }
                else if (sozdenFiziksele && i < sayi / 2)
                { gun = _rng.Next(30, 56); tur = Sec(OlayTurleri.SozelOlaylar); siddet = _rng.Next(1, 4); }
                else if (sozdenFiziksele)
                { gun = _rng.Next(0, 28); tur = Sec(OlayTurleri.FizikselOlaylar); siddet = _rng.Next(3, 6); }
                else
                { gun = _rng.Next(0, 56); tur = Sec(OlayTurleri.Tumunu); siddet = risk >= 4 ? _rng.Next(2, 6) : _rng.Next(1, 4); }

                var o = new Olay
                {
                    MahkumId = mid, OlayTarihi = DateTime.Today.AddDays(-gun), OlayTuru = tur,
                    Siddet = siddet, Hedef = Sec(Hedefler.Tumunu), ZamanDilimi = Sec(ZamanDilimleri.Tumunu),
                    Aciklama = OlayAciklama(tur), GirenKullanici = "disiplin1", GirenRol = "disiplin"
                };
                VeriDepolamaServisi.Instance.OlayKaydet(o);
            }
        }

        private static void PsikologOlustur(int mid, int risk)
        {
            int sayi = _rng.Next(2, 5);
            for (int i = 0; i < sayi; i++)
            {
                int gun = i * 14 + _rng.Next(0, 7);
                string durum; int agr, zar, isb; string ruh;
                if (risk >= 4 && i == 0)
                { durum = "Kötüleşme"; agr = _rng.Next(3, 6); zar = risk == 5 ? _rng.Next(4, 6) : _rng.Next(2, 5); isb = _rng.Next(1, 3); ruh = Sec(new[] { "Agresif", "Gergin", "Endişeli" }); }
                else if (risk >= 3)
                { durum = i == 0 ? "Kötüleşme" : Sec(new[] { "Aynı", "Kötüleşme" }); agr = _rng.Next(2, 5); zar = _rng.Next(1, 4); isb = _rng.Next(2, 4); ruh = Sec(new[] { "Gergin", "Endişeli", "Kayıtsız" }); }
                else
                { durum = Sec(new[] { "İyileşme", "Aynı", "Aynı" }); agr = _rng.Next(1, 3); zar = _rng.Next(1, 3); isb = _rng.Next(3, 6); ruh = Sec(new[] { "Sakin", "Dengeli" }); }

                var pd = new PsikologDegerlendirme
                {
                    MahkumId = mid, DegerlendirmeTarihi = DateTime.Today.AddDays(-gun),
                    RuhHali = ruh, AgresyonDuzeyi = agr, KendineZararRiski = zar, Isbirligi = isb,
                    OncekiDurumaGore = durum, Aciklama = $"Rutin değerlendirme - {ruh.ToLower()}",
                    GirenKullanici = "psikolog1", GirenRol = "psikolog"
                };
                VeriDepolamaServisi.Instance.PsikologKaydet(pd);
            }
        }

        private static void RevirOlustur(int mid, int risk)
        {
            int sayi = _rng.Next(2, 4);
            for (int i = 0; i < sayi; i++)
            {
                int gun = i * 10 + _rng.Next(0, 5);
                string uyku, ilac, dav; int stres;
                if (risk >= 4 && i == 0)
                { uyku = Sec(new[] { "Bozuk", "Uykusuzluk" }); ilac = Sec(new[] { "Kısmi", "Reddediyor" }); stres = _rng.Next(3, 6); dav = "Negatif"; }
                else if (risk >= 3)
                { uyku = Sec(new[] { "Normal", "Bozuk" }); ilac = Sec(new[] { "Tam", "Kısmi" }); stres = _rng.Next(2, 5); dav = Sec(new[] { "Nötr", "Negatif" }); }
                else
                { uyku = "Normal"; ilac = "Tam"; stres = _rng.Next(1, 3); dav = Sec(new[] { "Pozitif", "Nötr" }); }

                var rk = new RevirKaydi
                {
                    MahkumId = mid, KayitTarihi = DateTime.Today.AddDays(-gun),
                    UykuDurumu = uyku, IlacUyumu = ilac, StresSeviyesi = stres, DavranisEtkisi = dav,
                    Aciklama = $"Rutin kontrol - {uyku.ToLower()} uyku", GirenKullanici = "revir1", GirenRol = "revir"
                };
                VeriDepolamaServisi.Instance.RevirKaydet(rk);
            }
        }

        private static void KurulOlustur(int mid, int risk)
        {
            int sayi = risk >= 3 ? _rng.Next(1, 3) : _rng.Next(0, 2);
            for (int i = 0; i < sayi; i++)
            {
                int gun = 14 + i * 21 + _rng.Next(0, 7);
                string tur = risk >= 4
                    ? Sec(new[] { KararTurleri.HucreCezasi, KararTurleri.GozlemAltinaAlma, KararTurleri.ZiyaretKisitlamasi })
                    : Sec(KararTurleri.Tumunu);
                var kk = new KurulKarari
                {
                    MahkumId = mid, KararTarihi = DateTime.Today.AddDays(-gun), KararTuru = tur,
                    SureGun = Sec(new[] { 3, 5, 7, 14, 21 }), KisaGerekce = KararGerekce(tur),
                    DetayliGerekce = $"Detaylı değerlendirme — {tur.ToLower()} kararı uygulanması önerilmektedir.",
                    GozdenGecirmeTarihi = DateTime.Today.AddDays(-gun + 30),
                    GirenKullanici = "yonetici1", GirenRol = "yonetici"
                };

                kk.OnayBilgisiOlustur();

                // Some old decisions are fully approved, some partially
                bool tamOnay = gun > 21 && _rng.Next(0, 3) > 0;
                bool kısmiOnay = !tamOnay && gun > 7;

                foreach (var onay in kk.Onaylar)
                {
                    if (tamOnay)
                    {
                        onay.Durum = OnayDurumlari.Onaylandi;
                        onay.Tarih = DateTime.Today.AddDays(-gun + _rng.Next(1, 5));
                        onay.Yorum = $"Uygun bulunmuştur. ({onay.Rol})";
                        onay.Kullanici = onay.Rol == Roller.Psikolog ? "psikolog1" : onay.Rol == Roller.Revir ? "revir1" : "disiplin1";
                        onay.KullaniciAd = onay.Rol == Roller.Psikolog ? "Dr. Ayşe Yılmaz" : onay.Rol == Roller.Revir ? "Hemşire Fatma Kaya" : "Gardiyan Mehmet Demir";
                    }
                    else if (kısmiOnay && onay.Rol != Roller.Disiplin)
                    {
                        onay.Durum = OnayDurumlari.Onaylandi;
                        onay.Tarih = DateTime.Today.AddDays(-gun + _rng.Next(1, 3));
                        onay.Yorum = $"Değerlendirildi, onay verildi. ({onay.Rol})";
                        onay.Kullanici = onay.Rol == Roller.Psikolog ? "psikolog1" : "revir1";
                        onay.KullaniciAd = onay.Rol == Roller.Psikolog ? "Dr. Ayşe Yılmaz" : "Hemşire Fatma Kaya";
                    }
                }

                kk.OnayDurumuGuncelle();
                VeriDepolamaServisi.Instance.KurulKarariKaydet(kk);
            }

            // One brand new pending decision for high-risk inmates
            if (risk >= 4)
            {
                var yeniKarar = new KurulKarari
                {
                    MahkumId = mid, KararTarihi = DateTime.Today, KararTuru = KararTurleri.GozlemAltinaAlma,
                    SureGun = 14, KisaGerekce = "Yüksek risk nedeniyle acil gözlem altına alma önerilir.",
                    DetayliGerekce = "Son dönemde artan olaylar ve psikolojik değerlendirme sonuçları doğrultusunda yakın gözlem gereklidir.",
                    GozdenGecirmeTarihi = DateTime.Today.AddDays(14),
                    GirenKullanici = "yonetici1", GirenRol = "yonetici",
                    OnayDurumu = OnayDurumlari.Beklemede
                };
                yeniKarar.OnayBilgisiOlustur();
                VeriDepolamaServisi.Instance.KurulKarariKaydet(yeniKarar);
            }
        }

        private static T Sec<T>(T[] d) => d[_rng.Next(d.Length)];
        private static string OlayAciklama(string t) => t switch
        {
            "Kavga" => "Koğuş içinde fiziksel altercation yaşandı.",
            "Fiziksel saldırı" => "Fiziksel saldırı girişimi.",
            "Sözel tehdit" => "Sözel tehdit ve hakaret.",
            "Huzursuzluk çıkarma" => "Yüksek sesle bağırma, huzursuzluk.",
            "İtaatsizlik" => "Personel talimatlarına uymayı reddetme.",
            "Kendine zarar verme" => "Kendine zarar verme girişimi.",
            "Eşyaya zarar verme" => "Eşyalara zarar verme.",
            _ => "Disiplin olayı."
        };
        private static string KararGerekce(string t) => t switch
        {
            "Hücre cezası" => "Tekrarlayan ihlaller nedeniyle hücre cezası.",
            "Uyarı" => "İlk ihlal — yazılı uyarı.",
            "Gözlem altına alma" => "Risk değerlendirmesi sonucu gözlem altına alındı.",
            "İyi hal indirimi" => "Olumlu davranış gelişimi.",
            "Psikolog yönlendirme" => "Psikolojik destek ihtiyacı.",
            "Blok değişikliği" => "Ortamdan uzaklaştırma.",
            "Ziyaret kısıtlaması" => "Disiplin ihlali — ziyaret kısıtlaması.",
            _ => "Kurul kararı alındı."
        };
    }
}
