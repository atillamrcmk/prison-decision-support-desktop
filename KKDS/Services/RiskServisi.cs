using System;
using System.Collections.Generic;
using System.Linq;
using KKDS.Data;
using KKDS.Models;

namespace KKDS.Services
{
    public class RiskServisi
    {
        private static readonly Lazy<RiskServisi> _instance = new(() => new RiskServisi());
        public static RiskServisi Instance => _instance.Value;

        private readonly IDataRepository _veri = JsonDataRepository.Instance;

        public RiskSkoru RiskSkoruHesapla(int mahkumId)
        {
            var olaylar = _veri.MahkumOlaylari(mahkumId);
            var psikologlar = _veri.MahkumPsikolog(mahkumId);
            var revirler = _veri.MahkumRevir(mahkumId);

            var bugun = DateTime.Today;
            var son30Gun = olaylar.Where(o => o.OlayTarihi >= bugun.AddDays(-30)).ToList();

            // Olay yoğunluğu (ağırlık: %35)
            double olayYogunlugu = Math.Min(son30Gun.Count / 10.0, 1.0);

            // Şiddet ortalaması (ağırlık: %25)
            double siddetOrt = son30Gun.Any() ? son30Gun.Average(o => o.Siddet) / 5.0 : 0;

            // Psikolog riski (ağırlık: %20)
            double psikologRiski = 0;
            var sonPsikolog = psikologlar.FirstOrDefault();
            if (sonPsikolog != null)
            {
                bool kotulesme = sonPsikolog.OncekiDurumaGore.Equals("Kötüleşme", StringComparison.OrdinalIgnoreCase);
                if (kotulesme && (sonPsikolog.AgresyonDuzeyi >= 4 || sonPsikolog.KendineZararRiski >= 4))
                    psikologRiski = 1.0;
                else if (kotulesme && (sonPsikolog.AgresyonDuzeyi >= 3 || sonPsikolog.KendineZararRiski >= 3))
                    psikologRiski = 0.6;
                else if (kotulesme)
                    psikologRiski = 0.3;
            }

            // Revir riski (ağırlık: %10)
            double revirRiski = 0;
            var sonRevir = revirler.FirstOrDefault();
            if (sonRevir != null)
            {
                bool uykuBozuk = sonRevir.UykuDurumu.Equals("Bozuk", StringComparison.OrdinalIgnoreCase) ||
                                 sonRevir.UykuDurumu.Equals("Uykusuzluk", StringComparison.OrdinalIgnoreCase);
                bool ilacSorun = sonRevir.IlacUyumu.Equals("Reddediyor", StringComparison.OrdinalIgnoreCase);

                if (uykuBozuk && sonRevir.StresSeviyesi >= 4 && ilacSorun)
                    revirRiski = 1.0;
                else if (uykuBozuk && sonRevir.StresSeviyesi >= 3)
                    revirRiski = 0.6;
                else if (uykuBozuk || sonRevir.StresSeviyesi >= 3)
                    revirRiski = 0.3;
            }

            // Trend artışı (ağırlık: %10)
            double trendArtisi = 0;
            var son2Hafta = olaylar.Count(o => o.OlayTarihi >= bugun.AddDays(-14));
            var onceki2Hafta = olaylar.Count(o => o.OlayTarihi >= bugun.AddDays(-28) && o.OlayTarihi < bugun.AddDays(-14));

            if (son2Hafta > onceki2Hafta * 2 && son2Hafta >= 3)
                trendArtisi = 1.0;
            else if (son2Hafta > onceki2Hafta * 1.5)
                trendArtisi = 0.6;
            else if (son2Hafta > onceki2Hafta)
                trendArtisi = 0.3;

            double toplam = Math.Round(
                (olayYogunlugu * 35 +
                 siddetOrt * 25 +
                 psikologRiski * 20 +
                 revirRiski * 10 +
                 trendArtisi * 10), 1);

            toplam = Math.Min(toplam, 100);

            string seviye;
            if (toplam >= 70) seviye = "Yüksek";
            else if (toplam >= 30) seviye = "Orta";
            else seviye = "Düşük";

            return new RiskSkoru
            {
                ToplamSkor = toplam,
                Seviye = seviye,
                OlayYogunlugu = Math.Round(olayYogunlugu * 100, 1),
                SiddetOrtalamasi = Math.Round(siddetOrt * 100, 1),
                PsikologRiski = Math.Round(psikologRiski * 100, 1),
                RevirRiski = Math.Round(revirRiski * 100, 1),
                TrendArtisi = Math.Round(trendArtisi * 100, 1)
            };
        }

        public Dictionary<int, string> GlobalRiskDagilimi()
        {
            var mahkumlar = _veri.Mahkumlar.Where(m => m.Durum == "aktif" || m.Durum == "yakin_izlem").ToList();
            var skorlar = new Dictionary<int, double>();

            foreach (var m in mahkumlar)
            {
                var skor = RiskSkoruHesapla(m.Id);
                skorlar[m.Id] = skor.ToplamSkor;
            }

            var sirali = skorlar.OrderByDescending(s => s.Value).ToList();
            int toplamVaka = sirali.Count;
            var dagilim = new Dictionary<int, string>();

            for (int i = 0; i < sirali.Count; i++)
            {
                double yuzde = toplamVaka > 0 ? (double)i / toplamVaka : 0;
                if (yuzde < 0.2) dagilim[sirali[i].Key] = "yuksek";
                else if (yuzde < 0.7) dagilim[sirali[i].Key] = "orta";
                else dagilim[sirali[i].Key] = "dusuk";
            }

            return dagilim;
        }
    }
}
