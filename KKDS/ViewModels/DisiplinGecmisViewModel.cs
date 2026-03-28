using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class DisiplinGecmisViewModel : BaseViewModel
    {
        private string _aramaMetni = "";
        private DateTime? _baslangicTarihi;
        private DateTime? _bitisTarihi;

        public string AramaMetni { get => _aramaMetni; set { SetProperty(ref _aramaMetni, value); Filtrele(); } }
        public DateTime? BaslangicTarihi { get => _baslangicTarihi; set { SetProperty(ref _baslangicTarihi, value); Filtrele(); } }
        public DateTime? BitisTarihi { get => _bitisTarihi; set { SetProperty(ref _bitisTarihi, value); Filtrele(); } }
        public ObservableCollection<Olay> Kayitlar { get; } = new();
        public ICommand TemizleCommand { get; }

        public DisiplinGecmisViewModel()
        {
            TemizleCommand = new RelayCommand(() => { AramaMetni = ""; BaslangicTarihi = null; BitisTarihi = null; });
            Filtrele();
        }

        private void Filtrele()
        {
            Kayitlar.Clear();
            var list = VeriDepolamaServisi.Instance.KullaniciOlaylari(OturumBilgisi.Instance.KullaniciAdi);
            if (!string.IsNullOrWhiteSpace(AramaMetni))
                list = list.Where(k => k.MahkumKodu.Contains(AramaMetni, StringComparison.OrdinalIgnoreCase)).ToList();
            if (BaslangicTarihi.HasValue) list = list.Where(k => k.OlayTarihi >= BaslangicTarihi.Value).ToList();
            if (BitisTarihi.HasValue) list = list.Where(k => k.OlayTarihi <= BitisTarihi.Value).ToList();
            foreach (var k in list) Kayitlar.Add(k);
        }
    }
}
