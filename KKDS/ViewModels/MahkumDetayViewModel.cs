using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class MahkumDetayViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;
        private int _seciliSekme;

        public Mahkum Mahkum { get; set; } = new();
        public AnalizSonuc Analiz { get; set; } = new();
        public TahminSonuc Tahmin { get; set; } = new();
        public RiskSkoru Risk { get; set; } = new();

        public int SeciliSekme
        {
            get => _seciliSekme;
            set => SetProperty(ref _seciliSekme, value);
        }

        public ObservableCollection<HaftalikTrend> HaftalikTrendler { get; } = new();
        public ObservableCollection<OlayTurDagilim> OlayTurDagilimi { get; } = new();
        public ObservableCollection<Olay> Olaylar { get; } = new();
        public ObservableCollection<PsikologDegerlendirme> PsikologKayitlari { get; } = new();
        public ObservableCollection<RevirKaydi> RevirKayitlari { get; } = new();
        public ObservableCollection<KurulKarari> KurulKararlari { get; } = new();

        public int ToplamOlay { get; set; }
        public int KararOncesiOlay { get; set; }
        public int KararSonrasiOlay { get; set; }
        public string PsikologDurum { get; set; } = "-";
        public string RevirDurum { get; set; } = "-";

        public ICommand GeriCommand { get; }
        public ICommand SekmeCommand { get; }

        public MahkumDetayViewModel(int mahkumId, MainViewModel main)
        {
            _main = main;
            GeriCommand = new RelayCommand(() => main.MahkumListeGoster());
            SekmeCommand = new RelayCommand(p =>
            {
                if (p is string s && int.TryParse(s, out int idx))
                    SeciliSekme = idx;
            });

            Yukle(mahkumId);
        }

        private void Yukle(int mahkumId)
        {
            var veri = VeriDepolamaServisi.Instance;
            var analizServisi = AnalizServisi.Instance;
            var tahminServisi = TahminServisi.Instance;
            var riskServisi = RiskServisi.Instance;

            Mahkum = veri.Mahkumlar.FirstOrDefault(m => m.Id == mahkumId) ?? new Mahkum();
            Analiz = analizServisi.TamAnaliz(mahkumId);
            Tahmin = tahminServisi.YediGunlukTahmin(mahkumId);
            Risk = riskServisi.RiskSkoruHesapla(mahkumId);

            // Haftalık trendler
            foreach (var t in Analiz.HaftalikTrendler)
                HaftalikTrendler.Add(t);

            // Olay türü dağılımı
            var olaylar = veri.MahkumOlaylari(mahkumId);
            var gruplar = olaylar.GroupBy(o => o.OlayTuru).OrderByDescending(g => g.Count());
            foreach (var g in gruplar)
                OlayTurDagilimi.Add(new OlayTurDagilim { Tur = g.Key, Sayi = g.Count() });

            // Sekmeli ham veriler
            foreach (var o in olaylar) Olaylar.Add(o);
            foreach (var p in veri.MahkumPsikolog(mahkumId)) PsikologKayitlari.Add(p);
            foreach (var r in veri.MahkumRevir(mahkumId)) RevirKayitlari.Add(r);
            foreach (var k in veri.MahkumKararlari(mahkumId)) KurulKararlari.Add(k);

            // İstatistikler
            ToplamOlay = olaylar.Count;
            KararOncesiOlay = Analiz.KararEtkisi.OncesiOlaySayisi;
            KararSonrasiOlay = Analiz.KararEtkisi.SonrasiOlaySayisi;

            var sonPsikolog = veri.MahkumPsikolog(mahkumId).FirstOrDefault();
            PsikologDurum = sonPsikolog != null
                ? $"{sonPsikolog.RuhHali} - {sonPsikolog.OncekiDurumaGore}"
                : "Kayıt yok";

            var sonRevir = veri.MahkumRevir(mahkumId).FirstOrDefault();
            RevirDurum = sonRevir != null
                ? $"Uyku: {sonRevir.UykuDurumu}, Stres: {sonRevir.StresSeviyesi}/5"
                : "Kayıt yok";
        }
    }

    public class OlayTurDagilim
    {
        public string Tur { get; set; } = "";
        public int Sayi { get; set; }
    }
}
