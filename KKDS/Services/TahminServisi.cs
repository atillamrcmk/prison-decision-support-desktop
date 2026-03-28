using System;
using System.Collections.Generic;
using System.Linq;
using KKDS.Models;

namespace KKDS.Services
{
    public class TahminServisi
    {
        private static readonly Lazy<TahminServisi> _instance = new(() => new TahminServisi());
        public static TahminServisi Instance => _instance.Value;

        private readonly VeriDepolamaServisi _veri = VeriDepolamaServisi.Instance;

        public TahminSonuc YediGunlukTahmin(int mahkumId)
        {
            var olaylar = _veri.MahkumOlaylari(mahkumId);
            var psikologlar = _veri.MahkumPsikolog(mahkumId);
            var revirler = _veri.MahkumRevir(mahkumId);

            var bugun = DateTime.Today;
            var son14Gun = olaylar.Where(o => o.OlayTarihi >= bugun.AddDays(-14)).ToList();

            // Baz hesaplama
            double gunlukOrtalama = son14Gun.Count / 14.0;
            double bazTahmin = gunlukOrtalama * 7;

            var faktörler = new List<string>();
            double carpan = 1.0;
            double ekleme = 0;

            // Trend artışı
            var son7 = olaylar.Count(o => o.OlayTarihi >= bugun.AddDays(-7));
            var onceki7 = olaylar.Count(o => o.OlayTarihi >= bugun.AddDays(-14) && o.OlayTarihi < bugun.AddDays(-7));

            if (son7 > onceki7 && onceki7 > 0)
            {
                double artisOrani = (double)(son7 - onceki7) / onceki7;
                double trendCarpani = Math.Min(1 + artisOrani * 0.5, 1.5);
                carpan *= trendCarpani;
                faktörler.Add($"Trend artışı (x{trendCarpani:F2})");
            }
            else if (onceki7 == 0 && son7 > 0)
            {
                carpan *= 1.3;
                faktörler.Add("Sessiz dönem sonrası ani başlangıç (x1.30)");
            }

            // Psikolog etkisi
            var sonPsikolog = psikologlar.FirstOrDefault();
            if (sonPsikolog != null)
            {
                bool kotulesme = sonPsikolog.OncekiDurumaGore.Equals("Kötüleşme", StringComparison.OrdinalIgnoreCase);
                if (kotulesme)
                {
                    if (sonPsikolog.AgresyonDuzeyi >= 4)
                    {
                        ekleme += 2;
                        faktörler.Add("Psikolog: Yüksek agresyon (+2)");
                    }
                    else if (sonPsikolog.AgresyonDuzeyi >= 3)
                    {
                        ekleme += 1;
                        faktörler.Add("Psikolog: Orta agresyon (+1)");
                    }
                    else
                    {
                        ekleme += 0.5;
                        faktörler.Add("Psikolog: Kötüleşme (+0.5)");
                    }
                }
            }

            // Revir etkisi
            var sonRevir = revirler.FirstOrDefault();
            if (sonRevir != null)
            {
                bool uykuBozuk = sonRevir.UykuDurumu.Equals("Bozuk", StringComparison.OrdinalIgnoreCase) ||
                                 sonRevir.UykuDurumu.Equals("Uykusuzluk", StringComparison.OrdinalIgnoreCase);
                if (uykuBozuk && sonRevir.StresSeviyesi >= 4)
                {
                    ekleme += 1.5;
                    faktörler.Add("Revir: Uyku bozuk + yüksek stres (+1.5)");
                }
                else if (sonRevir.StresSeviyesi >= 3)
                {
                    ekleme += 0.8;
                    faktörler.Add("Revir: Orta-yüksek stres (+0.8)");
                }
            }

            // Yüksek şiddet
            int yuksekSiddet = son14Gun.Count(o => o.Siddet >= 4);
            if (yuksekSiddet >= 2)
            {
                ekleme += 0.5;
                faktörler.Add($"Son 14 günde {yuksekSiddet} yüksek şiddetli olay (+0.5)");
            }

            // Tekrar eden pattern
            var tekrarKombo = son14Gun
                .GroupBy(o => $"{o.OlayTuru}|{o.ZamanDilimi}")
                .Any(g => g.Count() >= 3);
            if (tekrarKombo)
            {
                ekleme += 0.5;
                faktörler.Add("Tekrar eden olay-zaman kalıbı (+0.5)");
            }

            double beklenen = Math.Round(bazTahmin * carpan + ekleme, 1);
            beklenen = Math.Max(beklenen, 0);

            // Güven aralığı
            double altSinir = Math.Round(beklenen * 0.6, 1);
            double ustSinir = Math.Round(beklenen * 1.4 + 0.5, 1);

            // Olasılık
            double olasilik;
            if (beklenen >= 5) olasilik = 90;
            else if (beklenen >= 3) olasilik = 75;
            else if (beklenen >= 1.5) olasilik = 55;
            else if (beklenen >= 0.5) olasilik = 35;
            else if (beklenen > 0) olasilik = 15;
            else olasilik = 5;

            // En riskli zaman
            string enRiskliZaman = "Bilinmiyor";
            if (son14Gun.Any())
            {
                enRiskliZaman = son14Gun
                    .GroupBy(o => o.ZamanDilimi)
                    .OrderByDescending(g => g.Count())
                    .First().Key;
            }

            // Beklenen olay türü
            string beklenenTur = "Bilinmiyor";
            if (son14Gun.Any())
            {
                beklenenTur = son14Gun
                    .GroupBy(o => o.OlayTuru)
                    .OrderByDescending(g => g.Count())
                    .First().Key;
            }

            return new TahminSonuc
            {
                BeklenenOlaySayisi = beklenen,
                Olasilik = olasilik,
                EnRiskliZaman = enRiskliZaman,
                BeklenenOlayTuru = beklenenTur,
                AltSinir = altSinir,
                UstSinir = ustSinir,
                EtkiFactorleri = faktörler
            };
        }
    }
}
