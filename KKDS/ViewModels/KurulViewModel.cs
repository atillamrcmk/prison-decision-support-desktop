using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class KurulViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;

        public string Tarih { get; set; } = DateTime.Today.ToString("dd MMMM yyyy");
        public int ToplamVaka { get; set; }
        public int OncelikliSayisi { get; set; }
        public int IzlemSayisi { get; set; }
        public int StabilSayisi { get; set; }

        public ObservableCollection<KurulVakaSatir> OncelikliVakalar { get; } = new();
        public ObservableCollection<KurulVakaSatir> IzlemVakalari { get; } = new();
        public ObservableCollection<KurulVakaSatir> StabilVakalar { get; } = new();

        public ICommand DetayCommand { get; }

        public KurulViewModel(MainViewModel main)
        {
            _main = main;
            DetayCommand = new RelayCommand(p => DetayGoster(p));
            Yukle();
        }

        private void Yukle()
        {
            var veri = VeriDepolamaServisi.Instance;
            var analizServisi = AnalizServisi.Instance;
            var riskServisi = RiskServisi.Instance;
            var tahminServisi = TahminServisi.Instance;

            var aktifMahkumlar = veri.Mahkumlar
                .Where(m => m.Durum == "aktif" || m.Durum == "yakin_izlem")
                .ToList();

            foreach (var m in aktifMahkumlar)
            {
                var analiz = analizServisi.TamAnaliz(m.Id);
                var risk = riskServisi.RiskSkoruHesapla(m.Id);
                var tahmin = tahminServisi.YediGunlukTahmin(m.Id);
                var olaylar = veri.MahkumOlaylari(m.Id);

                var satir = new KurulVakaSatir
                {
                    MahkumId = m.Id,
                    MahkumKodu = m.MahkumKodu,
                    Blok = m.Blok,
                    RiskSkoru = risk.ToplamSkor,
                    RiskSeviye = risk.Seviye,
                    Son30GunOlay = olaylar.Count(o => o.OlayTarihi >= DateTime.Today.AddDays(-30)),
                    SonKarar = analiz.KararEtkisi.SonKararTuru,
                    Egilim = analiz.Egilim,
                    TekrarRiski = analiz.RiskBoyutlari.Tekrar,
                    KurulOnceligi = analiz.RiskBoyutlari.KurulOnceligi,
                    DavranissalRisk = analiz.RiskBoyutlari.Davranissal,
                    PsikolojikRisk = analiz.RiskBoyutlari.Psikolojik,
                    SaglikRisk = analiz.RiskBoyutlari.Saglik,
                    OncelikNedenleri = analiz.OncelikNedenleri,
                    SistemOzeti = analiz.SistemOzeti.FirstOrDefault() ?? "Veri yetersiz.",
                    BeklenenOlay = tahmin.BeklenenOlaySayisi
                };

                switch (analiz.RiskBoyutlari.KurulOnceligi)
                {
                    case "yuksek":
                        OncelikliVakalar.Add(satir);
                        break;
                    case "orta":
                        IzlemVakalari.Add(satir);
                        break;
                    default:
                        StabilVakalar.Add(satir);
                        break;
                }
            }

            // Sort each group by risk score descending
            SortCollection(OncelikliVakalar);
            SortCollection(IzlemVakalari);
            SortCollection(StabilVakalar);

            ToplamVaka = aktifMahkumlar.Count;
            OncelikliSayisi = OncelikliVakalar.Count;
            IzlemSayisi = IzlemVakalari.Count;
            StabilSayisi = StabilVakalar.Count;
        }

        private void SortCollection(ObservableCollection<KurulVakaSatir> col)
        {
            var sorted = col.OrderByDescending(x => x.RiskSkoru).ToList();
            col.Clear();
            foreach (var item in sorted) col.Add(item);
        }

        private void DetayGoster(object? param)
        {
            if (param is int id)
                _main.MahkumDetayGoster(id);
        }
    }

    public class KurulVakaSatir
    {
        public int MahkumId { get; set; }
        public string MahkumKodu { get; set; } = "";
        public string Blok { get; set; } = "";
        public double RiskSkoru { get; set; }
        public string RiskSeviye { get; set; } = "";
        public int Son30GunOlay { get; set; }
        public string SonKarar { get; set; } = "";
        public string Egilim { get; set; } = "";
        public string TekrarRiski { get; set; } = "";
        public string KurulOnceligi { get; set; } = "";
        public string DavranissalRisk { get; set; } = "";
        public string PsikolojikRisk { get; set; } = "";
        public string SaglikRisk { get; set; } = "";
        public System.Collections.Generic.List<string> OncelikNedenleri { get; set; } = new();
        public string SistemOzeti { get; set; } = "";
        public double BeklenenOlay { get; set; }
        public string OncelikNedenleriMetin => OncelikNedenleri.Any()
            ? string.Join("\n", OncelikNedenleri.Select(n => $"• {n}"))
            : "Belirgin risk faktörü yok";
    }
}
