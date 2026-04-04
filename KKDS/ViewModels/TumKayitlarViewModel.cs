using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class KayitSatir
    {
        public string KayitId { get; set; } = "";
        public string KayitTuru { get; set; } = "";
        public string MahkumKodu { get; set; } = "";
        public DateTime OlusturmaZamani { get; set; }
        public string GirenKullanici { get; set; } = "";
        public string GirenRol { get; set; } = "";
        public string Ozet { get; set; } = "";
    }

    public class TumKayitlarViewModel : BaseViewModel
    {
        private string _aramaMetni = "";
        private string _seciliTur = "Tümü";
        private DateTime? _baslangicTarihi;
        private DateTime? _bitisTarihi;
        private bool _demoDahil;

        public string AramaMetni { get => _aramaMetni; set { SetProperty(ref _aramaMetni, value); Filtrele(); } }
        public string SeciliTur { get => _seciliTur; set { SetProperty(ref _seciliTur, value); Filtrele(); } }
        public DateTime? BaslangicTarihi { get => _baslangicTarihi; set { SetProperty(ref _baslangicTarihi, value); Filtrele(); } }
        public DateTime? BitisTarihi { get => _bitisTarihi; set { SetProperty(ref _bitisTarihi, value); Filtrele(); } }
        public bool DemoKayitlariGoster { get => _demoDahil; set { SetProperty(ref _demoDahil, value); Filtrele(); } }

        public ObservableCollection<string> TurListesi { get; } = new() { "Tümü", "disiplin", "psikolog", "revir", "kurul" };
        public ObservableCollection<KayitSatir> Kayitlar { get; } = new();
        public ICommand TemizleCommand { get; }

        public TumKayitlarViewModel()
        {
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            TemizleCommand = new RelayCommand(() => { AramaMetni = ""; SeciliTur = "Tümü"; BaslangicTarihi = null; BitisTarihi = null; DemoKayitlariGoster = false; });
            if (!YetkisizMod) Filtrele();
        }

        private void Filtrele()
        {
            Kayitlar.Clear();
            var tumKayitlar = VeriDepolamaServisi.Instance.TumKayitlar(DemoKayitlariGoster);

            if (SeciliTur != "Tümü")
                tumKayitlar = tumKayitlar.Where(k => k.KayitTuru == SeciliTur).ToList();
            if (!string.IsNullOrWhiteSpace(AramaMetni))
                tumKayitlar = tumKayitlar.Where(k => k.MahkumKodu.Contains(AramaMetni, StringComparison.OrdinalIgnoreCase) ||
                                                      k.GirenKullanici.Contains(AramaMetni, StringComparison.OrdinalIgnoreCase)).ToList();
            if (BaslangicTarihi.HasValue) tumKayitlar = tumKayitlar.Where(k => k.OlusturmaZamani >= BaslangicTarihi.Value).ToList();
            if (BitisTarihi.HasValue) tumKayitlar = tumKayitlar.Where(k => k.OlusturmaZamani <= BitisTarihi.Value.AddDays(1)).ToList();

            foreach (var k in tumKayitlar.Take(200))
            {
                Kayitlar.Add(new KayitSatir
                {
                    KayitId = k.KayitId,
                    KayitTuru = TurAdi(k.KayitTuru),
                    MahkumKodu = k.MahkumKodu,
                    OlusturmaZamani = k.OlusturmaZamani,
                    GirenKullanici = k.GirenKullanici,
                    GirenRol = Roller.RolAdi(k.GirenRol),
                    Ozet = OzetUret(k)
                });
            }
        }

        private static string TurAdi(string t) => t switch
        {
            "disiplin" => "Disiplin",
            "psikolog" => "Psikolog",
            "revir" => "Revir",
            "kurul" => "Kurul",
            _ => t
        };

        private static string OzetUret(BaseKayit k) => k switch
        {
            Olay o => $"{o.OlayTuru} — Şiddet: {o.Siddet}",
            PsikologDegerlendirme p => $"{p.RuhHali} — {p.OncekiDurumaGore}",
            RevirKaydi r => $"Uyku: {r.UykuDurumu}, Stres: {r.StresSeviyesi}",
            KurulKarari kk => $"{kk.KararTuru} — {kk.SureGun} gün",
            _ => ""
        };
    }
}
