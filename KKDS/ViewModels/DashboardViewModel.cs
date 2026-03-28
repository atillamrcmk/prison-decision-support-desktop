using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;

        public int ToplamMahkum { get; set; }
        public int Son30GunOlay { get; set; }
        public int Son30GunKarar { get; set; }
        public int YakinIzlemSayisi { get; set; }

        public int OlumluVakaSayisi { get; set; }
        public int TekrarYukselisSayisi { get; set; }
        public string EnYogunZamanDilimi { get; set; } = "-";
        public string EnSikOlayTuru { get; set; } = "-";

        public string EnYuksekRiskMahkum { get; set; } = "-";
        public double EnYuksekRiskSkor { get; set; }
        public string EnHizliKotuMahkum { get; set; } = "-";
        public string EnTehlikeliOruntu { get; set; } = "-";

        public ObservableCollection<HaftalikTrend> HaftalikTrendler { get; set; } = new();
        public ObservableCollection<GlobalKararEtki> KararEtkileri { get; set; } = new();
        public ObservableCollection<MahkumOzetSatir> EnProblemliMahkumlar { get; set; } = new();
        public ObservableCollection<MahkumOzetSatir> HizliKotulesenler { get; set; } = new();
        public ObservableCollection<SonOlaySatir> SonOlaylar { get; set; } = new();

        public ICommand DetayCommand { get; }

        public DashboardViewModel(MainViewModel main)
        {
            _main = main;
            DetayCommand = new RelayCommand(p => DetayGoster(p));
            YukleVeriler();
        }

        private void YukleVeriler()
        {
            var veri = VeriDepolamaServisi.Instance;
            var analiz = AnalizServisi.Instance;
            var risk = RiskServisi.Instance;
            var bugun = DateTime.Today;

            ToplamMahkum = veri.Mahkumlar.Count;
            Son30GunOlay = veri.Olaylar.Count(o => o.OlayTarihi >= bugun.AddDays(-30));
            Son30GunKarar = veri.KurulKararlari.Count(k => k.KararTarihi >= bugun.AddDays(-30));
            YakinIzlemSayisi = veri.Mahkumlar.Count(m => m.Durum == "yakin_izlem");

            var tumOlaylar = veri.Olaylar.ToList();

            // En yoğun zaman dilimi
            if (tumOlaylar.Any())
            {
                EnYogunZamanDilimi = tumOlaylar
                    .GroupBy(o => o.ZamanDilimi)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                EnSikOlayTuru = tumOlaylar
                    .GroupBy(o => o.OlayTuru)
                    .OrderByDescending(g => g.Count())
                    .First().Key;
            }

            // Haftalık global trend
            var globalTrend = analiz.HaftalikTrendHesapla(tumOlaylar.OrderByDescending(o => o.OlayTarihi).ToList());
            foreach (var t in globalTrend) HaftalikTrendler.Add(t);

            // Global karar etki
            var kararEtki = analiz.GlobalKararEtkiHesapla();
            foreach (var ke in kararEtki) KararEtkileri.Add(ke);

            // Her mahkum için analiz
            int olumlu = 0, tekrarYukselis = 0;
            double maxRisk = 0;
            string maxRiskMahkum = "";
            string hizliKotuMahkum = "";
            string tehlikeliOruntu = "";

            var mahkumAnalizleri = new List<(Mahkum m, AnalizSonuc a, RiskSkoru r)>();

            foreach (var m in veri.Mahkumlar)
            {
                var a = analiz.TamAnaliz(m.Id);
                var r = risk.RiskSkoruHesapla(m.Id);
                mahkumAnalizleri.Add((m, a, r));

                if (a.KararEtkisi.KaliciEtki) olumlu++;
                if (a.KararEtkisi.TekrarYukselisi) tekrarYukselis++;

                if (r.ToplamSkor > maxRisk)
                {
                    maxRisk = r.ToplamSkor;
                    maxRiskMahkum = m.MahkumKodu;
                }

                if (a.HizliKotulesme)
                    hizliKotuMahkum = m.MahkumKodu;

                if (a.Oruntu.SozdenFizikseleEvrim && string.IsNullOrEmpty(tehlikeliOruntu))
                    tehlikeliOruntu = $"{m.MahkumKodu}: Sözelden fiziksele evrim";
            }

            OlumluVakaSayisi = olumlu;
            TekrarYukselisSayisi = tekrarYukselis;
            EnYuksekRiskMahkum = maxRiskMahkum;
            EnYuksekRiskSkor = maxRisk;
            EnHizliKotuMahkum = string.IsNullOrEmpty(hizliKotuMahkum) ? "Yok" : hizliKotuMahkum;
            EnTehlikeliOruntu = string.IsNullOrEmpty(tehlikeliOruntu) ? "Tespit edilmedi" : tehlikeliOruntu;

            // En problemli 5 mahkum
            var problemli = mahkumAnalizleri
                .OrderByDescending(x => x.r.ToplamSkor)
                .Take(5);

            foreach (var (m, a, r) in problemli)
            {
                var olaylar = veri.MahkumOlaylari(m.Id);
                EnProblemliMahkumlar.Add(new MahkumOzetSatir
                {
                    MahkumId = m.Id,
                    MahkumKodu = m.MahkumKodu,
                    Blok = m.Blok,
                    RiskSkoru = r.ToplamSkor,
                    RiskSeviye = r.Seviye,
                    Son30GunOlay = olaylar.Count(o => o.OlayTarihi >= bugun.AddDays(-30)),
                    Egilim = a.Egilim,
                    SonKarar = a.KararEtkisi.SonKararTuru
                });
            }

            // Hızlı kötüleşenler
            var kotulesenler = mahkumAnalizleri.Where(x => x.a.HizliKotulesme || x.a.AniKirilma);
            foreach (var (m, a, r) in kotulesenler)
            {
                HizliKotulesenler.Add(new MahkumOzetSatir
                {
                    MahkumId = m.Id,
                    MahkumKodu = m.MahkumKodu,
                    Blok = m.Blok,
                    RiskSkoru = r.ToplamSkor,
                    RiskSeviye = r.Seviye,
                    Egilim = a.HizliKotulesme ? "Hızlı kötüleşme" : "Ani kırılma"
                });
            }

            // Son 10 olay
            var sonOlaylar = tumOlaylar.OrderByDescending(o => o.OlayTarihi).Take(10);
            foreach (var o in sonOlaylar)
            {
                var m = veri.Mahkumlar.FirstOrDefault(x => x.Id == o.MahkumId);
                SonOlaylar.Add(new SonOlaySatir
                {
                    Tarih = o.OlayTarihi.ToString("dd.MM.yyyy"),
                    MahkumKodu = m?.MahkumKodu ?? "-",
                    OlayTuru = o.OlayTuru,
                    Siddet = o.Siddet,
                    ZamanDilimi = o.ZamanDilimi,
                    MahkumId = o.MahkumId
                });
            }
        }

        private void DetayGoster(object? param)
        {
            if (param is int id)
                _main.MahkumDetayGoster(id);
        }
    }

    public class MahkumOzetSatir
    {
        public int MahkumId { get; set; }
        public string MahkumKodu { get; set; } = "";
        public string Blok { get; set; } = "";
        public double RiskSkoru { get; set; }
        public string RiskSeviye { get; set; } = "";
        public int Son30GunOlay { get; set; }
        public string Egilim { get; set; } = "";
        public string SonKarar { get; set; } = "";
    }

    public class SonOlaySatir
    {
        public string Tarih { get; set; } = "";
        public string MahkumKodu { get; set; } = "";
        public string OlayTuru { get; set; } = "";
        public int Siddet { get; set; }
        public string ZamanDilimi { get; set; } = "";
        public int MahkumId { get; set; }
    }
}
