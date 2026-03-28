using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class RevirFormViewModel : BaseViewModel
    {
        private string _mahkumKodu = "";
        private DateTime _tarih = DateTime.Today;
        private string _uykuDurumu = "Normal";
        private string _ilacUyumu = "Tam";
        private int _stres = 1;
        private string _davranisEtkisi = "Nötr";
        private string _aciklama = "";
        private string _mesaj = "";
        private bool _mesajHata;
        private bool _duzenleModu;
        private RevirKaydi? _secili;

        public string MahkumKodu { get => _mahkumKodu; set { SetProperty(ref _mahkumKodu, value); MahkumDegisti(); } }
        public DateTime Tarih { get => _tarih; set => SetProperty(ref _tarih, value); }
        public string UykuDurumu { get => _uykuDurumu; set => SetProperty(ref _uykuDurumu, value); }
        public string IlacUyumu { get => _ilacUyumu; set => SetProperty(ref _ilacUyumu, value); }
        public int Stres { get => _stres; set => SetProperty(ref _stres, value); }
        public string DavranisEtkisi { get => _davranisEtkisi; set => SetProperty(ref _davranisEtkisi, value); }
        public string Aciklama { get => _aciklama; set => SetProperty(ref _aciklama, value); }
        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }
        public bool DuzenleModu { get => _duzenleModu; set { SetProperty(ref _duzenleModu, value); OnPropertyChanged(nameof(FormBaslik)); } }
        public string FormBaslik => DuzenleModu ? "Kayıt Düzenleniyor" : "Yeni Revir Kaydı";

        public ObservableCollection<string> UykuListesi { get; } = new(UykuDurumlari.Tumunu);
        public ObservableCollection<string> IlacListesi { get; } = new(IlacUyumlari.Tumunu);
        public ObservableCollection<string> DavranisListesi { get; } = new(DavranisEtkileri.Tumunu);
        public ObservableCollection<RevirKaydi> GecmisKayitlar { get; } = new();

        public ICommand KaydetCommand { get; }
        public ICommand GuncelleCommand { get; }
        public ICommand TemizleCommand { get; }
        public ICommand SilCommand { get; }
        public ICommand KayitSecCommand { get; }

        public RevirFormViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet);
            GuncelleCommand = new RelayCommand(Guncelle, () => DuzenleModu);
            TemizleCommand = new RelayCommand(Temizle);
            SilCommand = new RelayCommand(Sil, () => DuzenleModu);
            KayitSecCommand = new RelayCommand(p => KayitSec(p));
        }

        private void MahkumDegisti()
        {
            GecmisKayitlar.Clear();
            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) return;
            foreach (var k in VeriDepolamaServisi.Instance.MahkumRevir(m.Id)) GecmisKayitlar.Add(k);
        }

        private void Kaydet()
        {
            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) { Msg("Mahkum bulunamadı.", true); return; }
            var rk = new RevirKaydi
            {
                MahkumId = m.Id, KayitTarihi = Tarih, UykuDurumu = UykuDurumu,
                IlacUyumu = IlacUyumu, StresSeviyesi = Stres, DavranisEtkisi = DavranisEtkisi, Aciklama = Aciklama
            };
            VeriDepolamaServisi.Instance.RevirKaydet(rk);
            Msg($"{MahkumKodu} için revir kaydı oluşturuldu.", false);
            Temizle(); MahkumDegisti();
        }

        private void Guncelle()
        {
            if (_secili == null) return;
            _secili.KayitTarihi = Tarih; _secili.UykuDurumu = UykuDurumu; _secili.IlacUyumu = IlacUyumu;
            _secili.StresSeviyesi = Stres; _secili.DavranisEtkisi = DavranisEtkisi; _secili.Aciklama = Aciklama;
            VeriDepolamaServisi.Instance.RevirKaydet(_secili);
            Msg("Kayıt güncellendi.", false); Temizle(); MahkumDegisti();
        }

        private void Sil()
        {
            if (_secili == null) return;
            VeriDepolamaServisi.Instance.RevirSoftDelete(_secili.KayitId);
            Msg("Kayıt pasife alındı.", false); Temizle(); MahkumDegisti();
        }

        private void KayitSec(object? p)
        {
            if (p is not RevirKaydi k) return;
            _secili = k; DuzenleModu = true;
            Tarih = k.KayitTarihi; UykuDurumu = k.UykuDurumu; IlacUyumu = k.IlacUyumu;
            Stres = k.StresSeviyesi; DavranisEtkisi = k.DavranisEtkisi; Aciklama = k.Aciklama;
        }

        private void Temizle()
        {
            _secili = null; DuzenleModu = false;
            Tarih = DateTime.Today; UykuDurumu = "Normal"; IlacUyumu = "Tam"; Stres = 1; DavranisEtkisi = "Nötr"; Aciklama = "";
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
