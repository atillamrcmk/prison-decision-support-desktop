using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KKDS.Models;

namespace KKDS.Services
{
    public class AnalizServisi
    {
        private static readonly Lazy<AnalizServisi> _instance = new(() => new AnalizServisi());
        public static AnalizServisi Instance => _instance.Value;

        private readonly VeriDepolamaServisi _veri = VeriDepolamaServisi.Instance;

        private static readonly string[] CubukRenkPaleti =
        {
            "#2563EB", "#7C3AED", "#DB2777", "#EA580C",
            "#CA8A04", "#059669", "#0D9488", "#4F46E5"
        };

        private static string CubukRengi(int sira) => CubukRenkPaleti[sira % CubukRenkPaleti.Length];

        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>Haftalık dilim [bas, sonUstHaric) için eksen etiketi (gerçek tarihler).</summary>
        private static string HaftaAraligiEtiketi(DateTime bas, DateTime sonUstHaric)
        {
            var son = sonUstHaric.Date.AddDays(-1);
            if (bas.Year == son.Year)
                return $"{bas.ToString("dd.MM", Tr)} – {son.ToString("dd.MM.yyyy", Tr)}";
            return $"{bas:dd.MM.yyyy} – {son:dd.MM.yyyy}";
        }

        public AnalizSonuc TamAnaliz(int mahkumId)
        {
            var olaylar = _veri.MahkumOlaylari(mahkumId);
            var psikologlar = _veri.MahkumPsikolog(mahkumId);
            var revirler = _veri.MahkumRevir(mahkumId);
            var kararlar = _veri.MahkumKararlari(mahkumId);

            var sonuc = new AnalizSonuc();

            sonuc.HaftalikTrendler = HaftalikTrendHesapla(olaylar);
            sonuc.Egilim = EgilimHesapla(sonuc.HaftalikTrendler);
            sonuc.HizliKotulesme = HizliKotulesmeKontrol(sonuc.HaftalikTrendler);
            sonuc.AniKirilma = AniKirilmaKontrol(sonuc.HaftalikTrendler);
            sonuc.Oruntu = OruntuAnaliziYap(olaylar);
            sonuc.KararEtkisi = KararEtkiAnaliziYap(olaylar, kararlar);
            sonuc.Uyum = UyumAnaliziYap(olaylar, psikologlar, revirler, sonuc.Egilim);
            sonuc.RiskBoyutlari = RiskBoyutlariHesapla(sonuc, psikologlar, revirler, olaylar);
            sonuc.SistemOzeti = SistemOzetiUret(sonuc, psikologlar, revirler);
            sonuc.OncelikNedenleri = OncelikNedeniUret(sonuc, psikologlar, revirler, olaylar);

            return sonuc;
        }

        #region Olay trend serisi (günlük / haftalık / aylık)
        /// <summary>Seçilen periyoda göre son 8 dilimin olay sayıları (etiket + renk).</summary>
        public List<HaftalikTrend> OlayTrendSerisiHesapla(List<Olay> olaylar, OlayTrendPeriyot periyot) =>
            periyot switch
            {
                OlayTrendPeriyot.Gunluk => GunlukTrendHesapla(olaylar),
                OlayTrendPeriyot.Aylik => AylikTrendHesapla(olaylar),
                _ => HaftalikTrendHesapla(olaylar)
            };

        /// <summary>Son 8 gün; eksen etiketi ilgili takvim günü (dd.MM.yyyy).</summary>
        public List<HaftalikTrend> GunlukTrendHesapla(List<Olay> olaylar)
        {
            var bugun = DateTime.Today;
            var trendler = new List<HaftalikTrend>();
            for (int k = 1; k <= 8; k++)
            {
                var gunBasi = bugun.AddDays(-8 + k).Date;
                var gunSonu = gunBasi.AddDays(1);
                var sayi = olaylar.Count(o => o.OlayTarihi >= gunBasi && o.OlayTarihi < gunSonu);
                trendler.Add(new HaftalikTrend
                {
                    Hafta = gunBasi.ToString("dd.MM.yyyy", Tr),
                    OlaySayisi = sayi,
                    CubukRenkHex = CubukRengi(k - 1)
                });
            }
            return trendler;
        }

        public List<HaftalikTrend> HaftalikTrendHesapla(List<Olay> olaylar)
        {
            var bugun = DateTime.Today;
            var trendler = new List<HaftalikTrend>();
            for (int i = 7; i >= 0; i--)
            {
                var haftaBasi = bugun.AddDays(-7 * (i + 1));
                var haftaSonu = bugun.AddDays(-7 * i);
                var sayi = olaylar.Count(o => o.OlayTarihi >= haftaBasi && o.OlayTarihi < haftaSonu);
                var idx = 8 - i - 1;
                trendler.Add(new HaftalikTrend
                {
                    Hafta = HaftaAraligiEtiketi(haftaBasi.Date, haftaSonu),
                    OlaySayisi = sayi,
                    CubukRenkHex = CubukRengi(idx)
                });
            }
            return trendler;
        }

        /// <summary>Son 8 ay; eksen etiketi ay adı + yıl (ör. Mart 2026).</summary>
        public List<HaftalikTrend> AylikTrendHesapla(List<Olay> olaylar)
        {
            var bugun = DateTime.Today;
            var trendler = new List<HaftalikTrend>();
            for (int k = 0; k < 8; k++)
            {
                var ayBasi = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-(7 - k));
                var aySonu = ayBasi.AddMonths(1);
                var sayi = olaylar.Count(o => o.OlayTarihi >= ayBasi && o.OlayTarihi < aySonu);
                var etiket = ayBasi.ToString("MMMM yyyy", Tr);
                if (etiket.Length > 0)
                    etiket = char.ToUpper(etiket[0], Tr) + etiket.Substring(1);
                trendler.Add(new HaftalikTrend
                {
                    Hafta = etiket,
                    OlaySayisi = sayi,
                    CubukRenkHex = CubukRengi(k)
                });
            }
            return trendler;
        }
        #endregion

        #region Egilim
        public string EgilimHesapla(List<HaftalikTrend> trendler)
        {
            if (trendler.Count < 6) return "stabil";

            var son3 = trendler.Skip(trendler.Count - 3).Take(3).Average(t => t.OlaySayisi);
            var onceki3 = trendler.Skip(trendler.Count - 6).Take(3).Average(t => t.OlaySayisi);

            if (onceki3 == 0)
                return son3 > 0 ? "artiyor" : "stabil";

            var fark = (son3 - onceki3) / onceki3;
            if (fark > 0.3) return "artiyor";
            if (fark < -0.3) return "azaliyor";
            return "stabil";
        }
        #endregion

        #region Hizli Kotulesme
        public bool HizliKotulesmeKontrol(List<HaftalikTrend> trendler)
        {
            if (trendler.Count < 6) return false;

            var son2 = trendler.Skip(trendler.Count - 2).Sum(t => t.OlaySayisi);
            var onceki4Ort = trendler.Skip(trendler.Count - 6).Take(4).Average(t => t.OlaySayisi);

            return son2 > onceki4Ort * 2 && son2 >= 3;
        }
        #endregion

        #region Ani Kirilma
        public bool AniKirilmaKontrol(List<HaftalikTrend> trendler)
        {
            if (trendler.Count < 2) return false;

            for (int i = 1; i < trendler.Count; i++)
            {
                if (trendler[i - 1].OlaySayisi <= 1 && trendler[i].OlaySayisi >= 3)
                    return true;
            }
            return false;
        }
        #endregion

        #region Oruntu Analizi
        public OruntuAnalizi OruntuAnaliziYap(List<Olay> olaylar)
        {
            var sonuc = new OruntuAnalizi();
            if (!olaylar.Any()) return sonuc;

            // En sık olay türü
            sonuc.EnSikOlayTuru = olaylar
                .GroupBy(o => o.OlayTuru)
                .OrderByDescending(g => g.Count())
                .First().Key;

            // En riskli zaman dilimi
            sonuc.EnRiskliZamanDilimi = olaylar
                .GroupBy(o => o.ZamanDilimi)
                .OrderByDescending(g => g.Count())
                .First().Key;

            // En sık hedef
            sonuc.EnSikHedef = olaylar
                .GroupBy(o => o.Hedef)
                .OrderByDescending(g => g.Count())
                .First().Key;

            // Sözelden fiziksele evrim
            sonuc.SozdenFizikseleEvrim = SozdenFizikseleKontrol(olaylar);

            // Tekrar eden döngü
            var combo = olaylar
                .GroupBy(o => $"{o.OlayTuru}|{o.ZamanDilimi}")
                .Where(g => g.Count() >= 3)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (combo != null)
            {
                sonuc.TekrarEdenDongu = true;
                sonuc.TekrarDonguDetay = $"{combo.Key.Split('|')[0]} - {combo.Key.Split('|')[1]} ({combo.Count()} kez)";
            }

            return sonuc;
        }

        private bool SozdenFizikseleKontrol(List<Olay> olaylar)
        {
            if (olaylar.Count < 4) return false;

            var siralanmis = olaylar.OrderBy(o => o.OlayTarihi).ToList();
            int orta = siralanmis.Count / 2;

            var ilkYari = siralanmis.Take(orta).ToList();
            var sonYari = siralanmis.Skip(orta).ToList();

            int ilkSozel = ilkYari.Count(o => OlayTurleri.SozelOlaylar.Contains(o.OlayTuru));
            int sonFiziksel = sonYari.Count(o => OlayTurleri.FizikselOlaylar.Contains(o.OlayTuru));
            int sonSozel = sonYari.Count(o => OlayTurleri.SozelOlaylar.Contains(o.OlayTuru));

            return ilkSozel >= 2 && sonFiziksel >= 2 && sonFiziksel > sonSozel;
        }
        #endregion

        #region Karar Etki Analizi
        public KararEtkiAnalizi KararEtkiAnaliziYap(List<Olay> olaylar, List<KurulKarari> kararlar)
        {
            var sonuc = new KararEtkiAnalizi();
            if (!kararlar.Any()) return sonuc;

            var sonKarar = kararlar.OrderByDescending(k => k.KararTarihi).First();
            sonuc.SonKararTuru = sonKarar.KararTuru;

            var kararTarihi = sonKarar.KararTarihi;
            var oncesi = olaylar.Count(o => o.OlayTarihi >= kararTarihi.AddDays(-14) && o.OlayTarihi < kararTarihi);
            var sonrasi = olaylar.Count(o => o.OlayTarihi >= kararTarihi && o.OlayTarihi <= kararTarihi.AddDays(14));

            sonuc.OncesiOlaySayisi = oncesi;
            sonuc.SonrasiOlaySayisi = sonrasi;

            // Kısa vadeli etki
            sonuc.KisaVadeliEtki = sonrasi < oncesi;

            // Kalıcı etki
            if (sonuc.KisaVadeliEtki)
            {
                var gunSayisiSonrasi = (DateTime.Today - kararTarihi).TotalDays;
                if (gunSayisiSonrasi > 0)
                {
                    var gunlukOranSonrasi = olaylar.Count(o => o.OlayTarihi >= kararTarihi) / gunSayisiSonrasi;
                    var gunlukOranOncesi = oncesi > 0 ? oncesi / 14.0 : 0;

                    var son3HaftaOlaylar = olaylar.Count(o => o.OlayTarihi >= DateTime.Today.AddDays(-21));
                    var onceki3HaftaOlaylar = olaylar.Count(o => o.OlayTarihi >= DateTime.Today.AddDays(-42) && o.OlayTarihi < DateTime.Today.AddDays(-21));

                    bool son3HaftaArtis = son3HaftaOlaylar > onceki3HaftaOlaylar;
                    sonuc.KaliciEtki = gunlukOranSonrasi <= gunlukOranOncesi && !son3HaftaArtis;

                    // Tekrar yükseliş
                    sonuc.TekrarYukselisi = sonuc.KisaVadeliEtki && son3HaftaArtis;
                }
            }

            return sonuc;
        }
        #endregion

        #region Global Karar Etki
        public List<GlobalKararEtki> GlobalKararEtkiHesapla()
        {
            var tumOlaylar = _veri.Olaylar.ToList();
            var tumKararlar = _veri.KurulKararlari.ToList();
            var sonuclar = new List<GlobalKararEtki>();

            foreach (var kararTuru in KararTurleri.Tumunu)
            {
                var buTurKararlar = tumKararlar.Where(k => k.KararTuru == kararTuru).ToList();
                if (!buTurKararlar.Any()) continue;

                int olumlu = 0, olumsuz = 0, etkisiz = 0;
                double toplamDegisim = 0;

                foreach (var karar in buTurKararlar)
                {
                    var oncesi7 = tumOlaylar.Count(o =>
                        o.MahkumId == karar.MahkumId &&
                        o.OlayTarihi >= karar.KararTarihi.AddDays(-7) &&
                        o.OlayTarihi < karar.KararTarihi);

                    var sonrasi7 = tumOlaylar.Count(o =>
                        o.MahkumId == karar.MahkumId &&
                        o.OlayTarihi >= karar.KararTarihi &&
                        o.OlayTarihi <= karar.KararTarihi.AddDays(7));

                    if (sonrasi7 < oncesi7) olumlu++;
                    else if (sonrasi7 > oncesi7) olumsuz++;
                    else etkisiz++;

                    if (oncesi7 > 0)
                        toplamDegisim += ((double)(sonrasi7 - oncesi7) / oncesi7) * 100;
                }

                sonuclar.Add(new GlobalKararEtki
                {
                    KararTuru = kararTuru,
                    ToplamKarar = buTurKararlar.Count,
                    YuzdeDeğişim = buTurKararlar.Count > 0 ? Math.Round(toplamDegisim / buTurKararlar.Count, 1) : 0,
                    OlumluSayi = olumlu,
                    OlumsuzSayi = olumsuz,
                    EtkisizSayi = etkisiz
                });
            }

            return sonuclar;
        }
        #endregion

        #region Uyum Analizi
        public UyumAnalizi UyumAnaliziYap(List<Olay> olaylar, List<PsikologDegerlendirme> psikologlar,
            List<RevirKaydi> revirler, string egilim)
        {
            var sonuc = new UyumAnalizi();

            // Psikolog-olay uyumu
            var sonPsikolog = psikologlar.FirstOrDefault();
            if (sonPsikolog != null)
            {
                sonuc.PsikologOlayUyumu =
                    sonPsikolog.OncekiDurumaGore.Equals("Kötüleşme", StringComparison.OrdinalIgnoreCase) &&
                    egilim == "artiyor";
            }

            // Revir-olay uyumu
            var sonRevir = revirler.FirstOrDefault();
            if (sonRevir != null)
            {
                sonuc.RevirOlayUyumu =
                    sonRevir.UykuDurumu.Equals("Bozuk", StringComparison.OrdinalIgnoreCase) &&
                    sonRevir.StresSeviyesi >= 3 &&
                    egilim == "artiyor";
            }

            sonuc.CiftSinyal = sonuc.PsikologOlayUyumu && sonuc.RevirOlayUyumu;

            return sonuc;
        }
        #endregion

        #region Risk Boyutlari
        public RiskBoyutlari RiskBoyutlariHesapla(AnalizSonuc analiz,
            List<PsikologDegerlendirme> psikologlar, List<RevirKaydi> revirler, List<Olay> olaylar)
        {
            var rb = new RiskBoyutlari();
            var sonPsikolog = psikologlar.FirstOrDefault();
            var sonRevir = revirler.FirstOrDefault();

            // Davranışsal
            if (analiz.HizliKotulesme || (analiz.Egilim == "artiyor" && analiz.Oruntu.SozdenFizikseleEvrim))
                rb.Davranissal = "yuksek";
            else if (analiz.Egilim == "artiyor" || analiz.Oruntu.SozdenFizikseleEvrim)
                rb.Davranissal = "orta";

            // Psikolojik
            if (sonPsikolog != null)
            {
                bool kotulesme = sonPsikolog.OncekiDurumaGore.Equals("Kötüleşme", StringComparison.OrdinalIgnoreCase);
                if (kotulesme && (sonPsikolog.AgresyonDuzeyi >= 4 || sonPsikolog.KendineZararRiski >= 4))
                    rb.Psikolojik = "yuksek";
                else if (kotulesme || sonPsikolog.AgresyonDuzeyi >= 3 || sonPsikolog.KendineZararRiski >= 3)
                    rb.Psikolojik = "orta";
            }

            // Sağlık
            if (sonRevir != null)
            {
                bool davranisEtkisi = sonRevir.DavranisEtkisi.Equals("Negatif", StringComparison.OrdinalIgnoreCase);
                if (davranisEtkisi && sonRevir.StresSeviyesi >= 4)
                    rb.Saglik = "yuksek";
                else if (davranisEtkisi || sonRevir.StresSeviyesi >= 3)
                    rb.Saglik = "orta";
            }

            // Tekrar
            if (analiz.KararEtkisi.TekrarYukselisi || (analiz.KararEtkisi.KisaVadeliEtki && !analiz.KararEtkisi.KaliciEtki))
                rb.Tekrar = "yuksek";
            else if (analiz.Oruntu.TekrarEdenDongu)
                rb.Tekrar = "orta";

            // Kurul önceliği
            var boyutlar = new[] { rb.Davranissal, rb.Psikolojik, rb.Saglik, rb.Tekrar };
            int yuksekSayisi = boyutlar.Count(b => b == "yuksek");
            int ortaSayisi = boyutlar.Count(b => b == "orta");

            if (yuksekSayisi >= 2 || (yuksekSayisi >= 1 && analiz.Uyum.CiftSinyal))
                rb.KurulOnceligi = "yuksek";
            else if (yuksekSayisi >= 1 || ortaSayisi >= 2)
                rb.KurulOnceligi = "orta";

            // Yüksek şiddetli olay kontrolü
            var son30Gun = olaylar.Where(o => o.OlayTarihi >= DateTime.Today.AddDays(-30)).ToList();
            int yuksekSiddet = son30Gun.Count(o => o.Siddet >= 4);
            if (yuksekSiddet >= 5 && rb.KurulOnceligi != "yuksek")
                rb.KurulOnceligi = "yuksek";
            else if (yuksekSiddet >= 3 && rb.KurulOnceligi == "dusuk")
                rb.KurulOnceligi = "orta";

            return rb;
        }
        #endregion

        #region Sistem Ozeti
        public List<string> SistemOzetiUret(AnalizSonuc analiz,
            List<PsikologDegerlendirme> psikologlar, List<RevirKaydi> revirler)
        {
            var ozet = new List<string>();

            // Trend
            if (analiz.HizliKotulesme && analiz.AniKirilma)
                ozet.Add("Davranış kontrol dışına çıkma eğilimindedir. Ani ve hızlı bir kötüleşme tespit edilmiştir.");
            else if (analiz.HizliKotulesme)
                ozet.Add("Son dönemde hızlı bir kötüleşme gözlemlenmektedir. Olay sayısı belirgin şekilde artmıştır.");
            else if (analiz.AniKirilma)
                ozet.Add("Ani bir davranış kırılması tespit edilmiştir. Sakin bir dönemin ardından olaylarda beklenmedik artış yaşanmıştır.");
            else if (analiz.Egilim == "artiyor")
                ozet.Add("Olay sayılarında artış eğilimi devam etmektedir.");
            else if (analiz.Egilim == "azaliyor")
                ozet.Add("Olay sayılarında azalma eğilimi gözlemlenmektedir. Mevcut durum olumlu seyretmektedir.");
            else
                ozet.Add("Olay sayıları stabil seyretmektedir.");

            // Örüntü
            if (analiz.Oruntu.SozdenFizikseleEvrim)
                ozet.Add("Sözlü tehditlerden fiziksel eylemlere geçiş örüntüsü tespit edilmiştir. Bu durum ciddi risk taşımaktadır.");

            if (analiz.Oruntu.TekrarEdenDongu)
                ozet.Add($"Tekrar eden davranış döngüsü tespit edilmiştir: {analiz.Oruntu.TekrarDonguDetay}");

            // Karar etkisi
            if (analiz.KararEtkisi.TekrarYukselisi)
                ozet.Add($"Son kurul kararı ({analiz.KararEtkisi.SonKararTuru}) kısa vadede etki göstermiş ancak tekrar yükselişe geçilmiştir.");
            else if (analiz.KararEtkisi.KaliciEtki)
                ozet.Add($"Son kurul kararı ({analiz.KararEtkisi.SonKararTuru}) kalıcı olumlu etki göstermektedir.");
            else if (analiz.KararEtkisi.KisaVadeliEtki)
                ozet.Add($"Son kurul kararı ({analiz.KararEtkisi.SonKararTuru}) kısa vadeli etki göstermiştir ancak kalıcılık henüz doğrulanmamıştır.");

            // Uyum
            if (analiz.Uyum.CiftSinyal)
                ozet.Add("Çoklu risk kaynağı mevcuttur. Hem psikolojik değerlendirme hem sağlık verileri kötüleşmeye işaret etmektedir.");
            else if (analiz.Uyum.PsikologOlayUyumu)
                ozet.Add("Psikolojik değerlendirme bulguları olay trendini desteklemektedir.");
            else if (analiz.Uyum.RevirOlayUyumu)
                ozet.Add("Revir verileri olay trendini desteklemektedir.");

            // Psikolog
            var sonPsikolog = psikologlar.FirstOrDefault();
            if (sonPsikolog != null && sonPsikolog.KendineZararRiski >= 4)
                ozet.Add("Acil yakın takip uygulanmalıdır. Kendine zarar verme riski yüksek düzeydedir.");

            // Kurul önceliği
            if (analiz.RiskBoyutlari.KurulOnceligi == "yuksek")
                ozet.Add("Bu vaka kurul gündeminde öncelikli değerlendirilmelidir.");
            else if (analiz.RiskBoyutlari.KurulOnceligi == "orta")
                ozet.Add("Bu vaka yakın izlemde tutulmalıdır.");

            return ozet;
        }
        #endregion

        #region Oncelik Nedeni
        public List<string> OncelikNedeniUret(AnalizSonuc analiz,
            List<PsikologDegerlendirme> psikologlar, List<RevirKaydi> revirler, List<Olay> olaylar)
        {
            var nedenler = new List<string>();

            if (analiz.HizliKotulesme)
                nedenler.Add("Son 2 haftada hızlı kötüleşme tespit edildi");

            if (analiz.AniKirilma)
                nedenler.Add("Ani davranış kırılması yaşandı");

            if (analiz.Oruntu.SozdenFizikseleEvrim)
                nedenler.Add("Sözelden fiziksele geçiş örüntüsü var");

            if (analiz.Uyum.CiftSinyal)
                nedenler.Add("Psikolog ve revir çift sinyal veriyor");

            if (analiz.KararEtkisi.TekrarYukselisi)
                nedenler.Add("Son kurul kararının etkisi geçici kalmış");

            var son30Gun = olaylar.Where(o => o.OlayTarihi >= DateTime.Today.AddDays(-30)).ToList();
            int yuksekSiddet = son30Gun.Count(o => o.Siddet >= 4);
            if (yuksekSiddet >= 3)
                nedenler.Add($"Yüksek şiddetli olay tekrarları ({yuksekSiddet} olay)");

            var sonPsikolog = psikologlar.FirstOrDefault();
            if (sonPsikolog != null && sonPsikolog.KendineZararRiski >= 4)
                nedenler.Add("Kendine zarar verme riski yüksek");

            if (analiz.Oruntu.TekrarEdenDongu)
                nedenler.Add("Tekrar eden davranış döngüsü mevcut");

            return nedenler;
        }
        #endregion
    }
}
