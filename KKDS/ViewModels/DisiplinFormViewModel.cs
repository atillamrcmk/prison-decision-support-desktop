using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class DisiplinFormViewModel : BaseViewModel
    {
        private string _mahkumKodu = "";
        private DateTime _tarih = DateTime.Today;
        private string _olayTuru = "";
        private int _siddet = 1;
        private string _hedef = "";
        private string _zamanDilimi = "Sabah";
        private string _aciklama = "";
        private string _mesaj = "";
        private bool _mesajHata;
        private bool _duzenleModu;
        private Olay? _secili;

        public string MahkumKodu { get => _mahkumKodu; set { SetProperty(ref _mahkumKodu, value); MahkumDegisti(); } }
        public DateTime Tarih { get => _tarih; set => SetProperty(ref _tarih, value); }
        public string OlayTuru { get => _olayTuru; set => SetProperty(ref _olayTuru, value); }
        public int Siddet { get => _siddet; set => SetProperty(ref _siddet, value); }
        public string Hedef { get => _hedef; set => SetProperty(ref _hedef, value); }
        public string ZamanDilimi { get => _zamanDilimi; set => SetProperty(ref _zamanDilimi, value); }
        public string Aciklama { get => _aciklama; set => SetProperty(ref _aciklama, value); }
        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }
        public bool DuzenleModu { get => _duzenleModu; set { SetProperty(ref _duzenleModu, value); OnPropertyChanged(nameof(FormBaslik)); } }
        public string FormBaslik => DuzenleModu ? "Kayıt Düzenleniyor" : "Yeni Olay Kaydı";

        public ObservableCollection<string> OlayTurleriListesi { get; } = new(OlayTurleri.Tumunu);
        public ObservableCollection<string> HedeflerListesi { get; } = new(Hedefler.Tumunu);
        public ObservableCollection<string> ZamanDilimleriListesi { get; } = new(ZamanDilimleri.Tumunu);
        public ObservableCollection<Olay> GecmisKayitlar { get; } = new();

        public ICommand KaydetCommand { get; }
        public ICommand GuncelleCommand { get; }
        public ICommand TemizleCommand { get; }
        public ICommand SilCommand { get; }
        public ICommand KayitSecCommand { get; }

        public DisiplinFormViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet);
            GuncelleCommand = new RelayCommand(Guncelle, () => DuzenleModu);
            TemizleCommand = new RelayCommand(Temizle);
            SilCommand = new RelayCommand(Sil, () => DuzenleModu);
            KayitSecCommand = new RelayCommand(p => KayitSec(p));
            YetkiServisi.ViewModelKoruma(this, Roller.Disiplin);
        }

        private void MahkumDegisti()
        {
            GecmisKayitlar.Clear();
            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) return;
            foreach (var k in VeriDepolamaServisi.Instance.MahkumOlaylari(m.Id)) GecmisKayitlar.Add(k);
        }

        private void Kaydet()
        {
            var hata = FormDogrulama.Birlestir(
                FormDogrulama.MahkumKoduKontrol(MahkumKodu),
                FormDogrulama.TarihKontrol(Tarih, "Olay tarihi"),
                FormDogrulama.ComboZorunlu(OlayTuru, "Olay türü"),
                FormDogrulama.ComboZorunlu(Hedef, "Hedef"),
                FormDogrulama.ComboZorunlu(ZamanDilimi, "Zaman dilimi"),
                FormDogrulama.AciklamaUzunluk(Aciklama));
            if (!string.IsNullOrEmpty(hata)) { Msg(hata, true); return; }

            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) { Msg("Mahkum bulunamadı.", true); return; }
            var o = new Olay
            {
                MahkumId = m.Id, OlayTarihi = Tarih, OlayTuru = OlayTuru,
                Siddet = Siddet, Hedef = Hedef, ZamanDilimi = ZamanDilimi, Aciklama = Aciklama
            };
            VeriDepolamaServisi.Instance.OlayKaydet(o);
            Msg($"{MahkumKodu} için disiplin olayı kaydedildi.", false);
            Temizle(); MahkumDegisti();
        }

        private void Guncelle()
        {
            if (_secili == null) return;
            _secili.OlayTarihi = Tarih; _secili.OlayTuru = OlayTuru; _secili.Siddet = Siddet;
            _secili.Hedef = Hedef; _secili.ZamanDilimi = ZamanDilimi; _secili.Aciklama = Aciklama;
            VeriDepolamaServisi.Instance.OlayKaydet(_secili);
            Msg("Kayıt güncellendi.", false); Temizle(); MahkumDegisti();
        }

        private void Sil()
        {
            if (_secili == null) return;
            VeriDepolamaServisi.Instance.OlaySoftDelete(_secili.KayitId);
            Msg("Kayıt pasife alındı.", false); Temizle(); MahkumDegisti();
        }

        private void KayitSec(object? p)
        {
            if (p is not Olay k) return;
            _secili = k; DuzenleModu = true;
            Tarih = k.OlayTarihi; OlayTuru = k.OlayTuru; Siddet = k.Siddet;
            Hedef = k.Hedef; ZamanDilimi = k.ZamanDilimi; Aciklama = k.Aciklama;
        }

        private void Temizle()
        {
            _secili = null; DuzenleModu = false;
            Tarih = DateTime.Today; OlayTuru = ""; Siddet = 1; Hedef = ""; ZamanDilimi = "Sabah"; Aciklama = "";
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
