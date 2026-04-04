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

        private int _toplamMahkum;
        private int _son30GunOlay;
        private int _son30GunKarar;
        private int _yakinIzlemSayisi;
        private int _olumluVakaSayisi;
        private int _tekrarYukselisSayisi;
        private string _enYogunZamanDilimi = "-";
        private string _enSikOlayTuru = "-";
        private string _enYuksekRiskMahkum = "-";
        private double _enYuksekRiskSkor;
        private string _enHizliKotuMahkum = "-";
        private string _enTehlikeliOruntu = "-";
        private string _sonYenilemeMetni = "";
        private string _operasyonelOzet = "";
        private bool _veriBos;
        private bool _yukleniyor;
        private bool _kritikUyariBos = true;
        private string _kritikUyariBaslik = "Kritik uyarılar";

        public int ToplamMahkum { get => _toplamMahkum; set => SetProperty(ref _toplamMahkum, value); }
        public int Son30GunOlay { get => _son30GunOlay; set => SetProperty(ref _son30GunOlay, value); }
        public int Son30GunKarar { get => _son30GunKarar; set => SetProperty(ref _son30GunKarar, value); }
        public int YakinIzlemSayisi { get => _yakinIzlemSayisi; set => SetProperty(ref _yakinIzlemSayisi, value); }
        public int OlumluVakaSayisi { get => _olumluVakaSayisi; set => SetProperty(ref _olumluVakaSayisi, value); }
        public int TekrarYukselisSayisi { get => _tekrarYukselisSayisi; set => SetProperty(ref _tekrarYukselisSayisi, value); }
        public string EnYogunZamanDilimi { get => _enYogunZamanDilimi; set => SetProperty(ref _enYogunZamanDilimi, value); }
        public string EnSikOlayTuru { get => _enSikOlayTuru; set => SetProperty(ref _enSikOlayTuru, value); }
        public string EnYuksekRiskMahkum { get => _enYuksekRiskMahkum; set => SetProperty(ref _enYuksekRiskMahkum, value); }
        public double EnYuksekRiskSkor { get => _enYuksekRiskSkor; set => SetProperty(ref _enYuksekRiskSkor, value); }
        public string EnHizliKotuMahkum { get => _enHizliKotuMahkum; set => SetProperty(ref _enHizliKotuMahkum, value); }
        public string EnTehlikeliOruntu { get => _enTehlikeliOruntu; set => SetProperty(ref _enTehlikeliOruntu, value); }
        public string SonYenilemeMetni { get => _sonYenilemeMetni; set => SetProperty(ref _sonYenilemeMetni, value); }
        public string OperasyonelOzet { get => _operasyonelOzet; set => SetProperty(ref _operasyonelOzet, value); }
        public bool VeriBos { get => _veriBos; set => SetProperty(ref _veriBos, value); }
        public bool Yukleniyor { get => _yukleniyor; set => SetProperty(ref _yukleniyor, value); }
        public bool KritikUyariBos { get => _kritikUyariBos; set => SetProperty(ref _kritikUyariBos, value); }
        public string KritikUyariBaslik { get => _kritikUyariBaslik; set => SetProperty(ref _kritikUyariBaslik, value); }

        public ObservableCollection<HaftalikTrend> HaftalikTrendler { get; } = new();
        public ObservableCollection<TrendPeriyotOgesi> TrendPeriyotSecenekleri { get; } = new()
        {
            new TrendPeriyotOgesi { Deger = OlayTrendPeriyot.Gunluk, Ad = "Günlük (8 gün)" },
            new TrendPeriyotOgesi { Deger = OlayTrendPeriyot.Haftalik, Ad = "Haftalık (8 hafta)" },
            new TrendPeriyotOgesi { Deger = OlayTrendPeriyot.Aylik, Ad = "Aylık (8 ay)" }
        };

        private OlayTrendPeriyot _trendPeriyot = OlayTrendPeriyot.Haftalik;
        private List<Olay> _olaylarTrendIcin = new();

        public OlayTrendPeriyot TrendPeriyot
        {
            get => _trendPeriyot;
            set
            {
                if (SetProperty(ref _trendPeriyot, value))
                {
                    OnPropertyChanged(nameof(TrendGrafikBaslik));
                    OnPropertyChanged(nameof(TrendGrafikAciklama));
                    GuncelleTrendSerisi();
                }
            }
        }

        public string TrendGrafikBaslik => TrendPeriyot switch
        {
            OlayTrendPeriyot.Gunluk => "Günlük olay trendi",
            OlayTrendPeriyot.Aylik => "Aylık olay trendi",
            _ => "Haftalık olay trendi"
        };

        public string TrendGrafikAciklama => TrendPeriyot switch
        {
            OlayTrendPeriyot.Gunluk => "Her sütun bir takvim gününün toplam olay sayısıdır (eksende o günün tarihi). Çubuklar ortak taban çizgisinde hizalanır.",
            OlayTrendPeriyot.Aylik => "Her sütun bir takvim ayının toplam olay sayısıdır (eksende ay ve yıl). Çubuklar ortak taban çizgisinde hizalanır.",
            _ => "Her sütun 7 günlük dilimin toplam olay sayısıdır (eksende haftanın başlangıç–bitiş tarihleri). Çubuklar ortak taban çizgisinde hizalanır."
        };

        public ObservableCollection<GlobalKararEtki> KararEtkileri { get; } = new();
        public ObservableCollection<MahkumOzetSatir> EnProblemliMahkumlar { get; } = new();
        public ObservableCollection<MahkumOzetSatir> HizliKotulesenler { get; } = new();
        public ObservableCollection<SonOlaySatir> SonOlaylar { get; } = new();
        public ObservableCollection<KritikUyariOgesi> KritikUyarilar { get; } = new();

        public ICommand DetayCommand { get; }
        public ICommand YenileCommand { get; }
        public ICommand DemoYukleCommand { get; }
        public ICommand DemoTemizleCommand { get; }

        public DashboardViewModel(MainViewModel main)
        {
            _main = main;
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            DetayCommand = new RelayCommand(p => DetayGoster(p));
            YenileCommand = new RelayCommand(() => YukleVeriler());
            DemoYukleCommand = new RelayCommand(DemoYukle);
            DemoTemizleCommand = new RelayCommand(DemoTemizle);
            if (!YetkisizMod) YukleVeriler();
        }

        private void DemoYukle()
        {
            try
            {
                VeriDepolamaServisi.Instance.DemoVerisiYukle();
                YukleVeriler();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Demo verisi yüklenemedi: " + ex.Message, "KKDS", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void DemoTemizle()
        {
            if (System.Windows.MessageBox.Show("Yalnızca demo olarak işaretlenen kayıtlar silinecek. Devam edilsin mi?", "KKDS",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;
            VeriDepolamaServisi.Instance.DemoVerisiniTemizle();
            YukleVeriler();
        }

        private void YukleVeriler()
        {
            Yukleniyor = true;
            try
            {
            var veri = VeriDepolamaServisi.Instance;
            veri.TumVerileriYukle();
            var analiz = AnalizServisi.Instance;
            var risk = RiskServisi.Instance;
            var bugun = DateTime.Today;
            // Üretim listeleri demo hariç; gösterge paneli demo yüklendiğinde de dolu görünsün diye demo dahil mahkum/olay kullanılır.
            var panoMahkumlar = veri.MahkumlariGetir(demoDahil: true, pasifDahil: false);

            SonYenilemeMetni = $"Son güncelleme: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            OperasyonelOzet = $"Toplam okunan kayıt dosyası: {veri.SonYuklemeOkunanDosyaSayisi} | Hatalı dosya: {veri.SonYuklemeHataliDosyaSayisi} | Veri yolu: {veri.VeriKokYolu}";

            HaftalikTrendler.Clear();
            KararEtkileri.Clear();
            EnProblemliMahkumlar.Clear();
            HizliKotulesenler.Clear();
            SonOlaylar.Clear();
            KritikUyarilar.Clear();
            foreach (var u in analiz.KritikUyarlariHesapla()) KritikUyarilar.Add(u);
            KritikUyariBos = KritikUyarilar.Count == 0;
            KritikUyariBaslik = KritikUyarilar.Count == 0
                ? "Kritik uyarılar"
                : $"Kritik uyarılar ({KritikUyarilar.Count})";

            ToplamMahkum = panoMahkumlar.Count;
            VeriBos = ToplamMahkum == 0;
            Son30GunOlay = veri.OlaylarDahilDemo.Count(o => o.OlayTarihi >= bugun.AddDays(-30));
            Son30GunKarar = veri.KurulKararlariDahilDemo.Count(k => k.KararTarihi >= bugun.AddDays(-30));
            YakinIzlemSayisi = panoMahkumlar.Count(m => m.Durum == "yakin_izlem");

            var tumOlaylar = veri.OlaylarDahilDemo.ToList();
            _olaylarTrendIcin = tumOlaylar;

            EnYogunZamanDilimi = "-";
            EnSikOlayTuru = "-";
            if (tumOlaylar.Any())
            {
                EnYogunZamanDilimi = tumOlaylar.GroupBy(o => o.ZamanDilimi).OrderByDescending(g => g.Count()).First().Key;
                EnSikOlayTuru = tumOlaylar.GroupBy(o => o.OlayTuru).OrderByDescending(g => g.Count()).First().Key;
            }

            GuncelleTrendSerisi();

            foreach (var ke in analiz.GlobalKararEtkiHesapla()) KararEtkileri.Add(ke);

            var analizCache = new Dictionary<int, (AnalizSonuc A, RiskSkoru R)>();
            foreach (var m in panoMahkumlar)
                analizCache[m.Id] = (analiz.TamAnaliz(m.Id), risk.RiskSkoruHesapla(m.Id));

            int olumlu = 0, tekrarYukselis = 0;
            double maxRisk = 0;
            string maxRiskMahkum = "";
            string hizliKotuMahkum = "";
            string tehlikeliOruntu = "";

            foreach (var m in panoMahkumlar)
            {
                var (a, r) = analizCache[m.Id];
                if (a.KararEtkisi.KaliciEtki) olumlu++;
                if (a.KararEtkisi.TekrarYukselisi) tekrarYukselis++;
                if (r.ToplamSkor > maxRisk) { maxRisk = r.ToplamSkor; maxRiskMahkum = m.MahkumKodu; }
                if (a.HizliKotulesme) hizliKotuMahkum = m.MahkumKodu;
                if (a.Oruntu.SozdenFizikseleEvrim && string.IsNullOrEmpty(tehlikeliOruntu))
                    tehlikeliOruntu = $"{m.MahkumKodu}: Sözelden fiziksele evrim";
            }

            OlumluVakaSayisi = olumlu;
            TekrarYukselisSayisi = tekrarYukselis;
            EnYuksekRiskMahkum = maxRiskMahkum;
            EnYuksekRiskSkor = maxRisk;
            EnHizliKotuMahkum = string.IsNullOrEmpty(hizliKotuMahkum) ? "Yok" : hizliKotuMahkum;
            EnTehlikeliOruntu = string.IsNullOrEmpty(tehlikeliOruntu) ? "Tespit edilmedi" : tehlikeliOruntu;

            var problemli = panoMahkumlar
                .Select(m => (Mahkum: m, R: analizCache[m.Id].R))
                .OrderByDescending(x => x.R.ToplamSkor)
                .Take(5);

            foreach (var (m, r) in problemli)
            {
                var a = analizCache[m.Id].A;
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

            foreach (var m in panoMahkumlar)
            {
                var a = analizCache[m.Id].A;
                var r = analizCache[m.Id].R;
                if (a.HizliKotulesme || a.AniKirilma)
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
            }

            foreach (var o in tumOlaylar.OrderByDescending(o => o.OlayTarihi).Take(10))
            {
                var mm = veri.MahkumBulById(o.MahkumId);
                SonOlaylar.Add(new SonOlaySatir
                {
                    Tarih = o.OlayTarihi.ToString("dd.MM.yyyy"),
                    MahkumKodu = mm?.MahkumKodu ?? "-",
                    OlayTuru = o.OlayTuru,
                    Siddet = o.Siddet,
                    ZamanDilimi = o.ZamanDilimi,
                    MahkumId = o.MahkumId
                });
            }
            }
            finally { Yukleniyor = false; }
        }

        private void GuncelleTrendSerisi()
        {
            HaftalikTrendler.Clear();
            foreach (var t in AnalizServisi.Instance.OlayTrendSerisiHesapla(_olaylarTrendIcin, TrendPeriyot))
                HaftalikTrendler.Add(t);
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
