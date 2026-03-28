using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class PsikologGecmisViewModel : BaseViewModel
    {
        private string _aramaMetni = "";
        private DateTime? _baslangicTarihi;
        private DateTime? _bitisTarihi;

        public string AramaMetni { get => _aramaMetni; set { SetProperty(ref _aramaMetni, value); Filtrele(); } }
        public DateTime? BaslangicTarihi { get => _baslangicTarihi; set { SetProperty(ref _baslangicTarihi, value); Filtrele(); } }
        public DateTime? BitisTarihi { get => _bitisTarihi; set { SetProperty(ref _bitisTarihi, value); Filtrele(); } }

        public ObservableCollection<PsikologDegerlendirme> Kayitlar { get; } = new();
        public ICommand FiltreleCommand { get; }
        public ICommand TemizleCommand { get; }

        public PsikologGecmisViewModel()
        {
            FiltreleCommand = new RelayCommand(Filtrele);
            TemizleCommand = new RelayCommand(() => { AramaMetni = ""; BaslangicTarihi = null; BitisTarihi = null; });
            Filtrele();
        }

        private void Filtrele()
        {
            Kayitlar.Clear();
            var kullanici = OturumBilgisi.Instance.KullaniciAdi;
            var kayitlar = VeriDepolamaServisi.Instance.KullaniciPsikologKayitlari(kullanici);

            if (!string.IsNullOrWhiteSpace(AramaMetni))
                kayitlar = kayitlar.Where(k => k.MahkumKodu.Contains(AramaMetni, StringComparison.OrdinalIgnoreCase)).ToList();
            if (BaslangicTarihi.HasValue)
                kayitlar = kayitlar.Where(k => k.DegerlendirmeTarihi >= BaslangicTarihi.Value).ToList();
            if (BitisTarihi.HasValue)
                kayitlar = kayitlar.Where(k => k.DegerlendirmeTarihi <= BitisTarihi.Value).ToList();

            foreach (var k in kayitlar) Kayitlar.Add(k);
        }
    }
}
