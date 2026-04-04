using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class PsikologFormViewModel : BaseViewModel
    {
        private string _mahkumKodu = "";
        private DateTime _tarih = DateTime.Today;
        private string _ruhHali = "";
        private int _agresyon = 1;
        private int _kendineZarar = 1;
        private int _isbirligi = 3;
        private string _oncekiDurum = "Aynı";
        private string _aciklama = "";
        private string _mesaj = "";
        private bool _mesajHata;
        private bool _duzenleModu;
        private PsikologDegerlendirme? _seciliKayit;

        public string MahkumKodu { get => _mahkumKodu; set { SetProperty(ref _mahkumKodu, value); MahkumKoduDegisti(); } }
        public DateTime Tarih { get => _tarih; set => SetProperty(ref _tarih, value); }
        public string RuhHali { get => _ruhHali; set => SetProperty(ref _ruhHali, value); }
        public int Agresyon { get => _agresyon; set => SetProperty(ref _agresyon, value); }
        public int KendineZarar { get => _kendineZarar; set => SetProperty(ref _kendineZarar, value); }
        public int Isbirligi { get => _isbirligi; set => SetProperty(ref _isbirligi, value); }
        public string OncekiDurum { get => _oncekiDurum; set => SetProperty(ref _oncekiDurum, value); }
        public string Aciklama { get => _aciklama; set => SetProperty(ref _aciklama, value); }
        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }
        public bool DuzenleModu { get => _duzenleModu; set { SetProperty(ref _duzenleModu, value); OnPropertyChanged(nameof(FormBaslik)); } }
        public string FormBaslik => DuzenleModu ? "Kayıt Düzenleniyor" : "Yeni Değerlendirme";

        public ObservableCollection<string> RuhHalleriListesi { get; } = new(RuhHalleri.Tumunu);
        public ObservableCollection<string> OncekiDurumListesi { get; } = new(OncekiDurumlar.Tumunu);
        public ObservableCollection<PsikologDegerlendirme> GecmisKayitlar { get; } = new();

        public ICommand KaydetCommand { get; }
        public ICommand GuncelleCommand { get; }
        public ICommand TemizleCommand { get; }
        public ICommand SilCommand { get; }
        public ICommand KayitSecCommand { get; }

        public PsikologFormViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet);
            GuncelleCommand = new RelayCommand(Guncelle, () => DuzenleModu);
            TemizleCommand = new RelayCommand(Temizle);
            SilCommand = new RelayCommand(Sil, () => DuzenleModu);
            KayitSecCommand = new RelayCommand(p => KayitSec(p));
            YetkiServisi.ViewModelKoruma(this, Roller.Psikolog);
        }

        private void MahkumKoduDegisti()
        {
            GecmisKayitlar.Clear();
            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) return;
            foreach (var k in VeriDepolamaServisi.Instance.MahkumPsikolog(m.Id))
                GecmisKayitlar.Add(k);
        }

        private void Kaydet()
        {
            var hata = FormDogrulama.Birlestir(
                FormDogrulama.MahkumKoduKontrol(MahkumKodu),
                FormDogrulama.TarihKontrol(Tarih, "Değerlendirme tarihi"),
                FormDogrulama.ComboZorunlu(RuhHali, "Ruh hali"),
                FormDogrulama.ComboZorunlu(OncekiDurum, "Önceki duruma göre"),
                FormDogrulama.AciklamaUzunluk(Aciklama));
            if (!string.IsNullOrEmpty(hata)) { MesajGoster(hata, true); return; }

            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) { MesajGoster($"'{MahkumKodu}' kodlu mahkum bulunamadı.", true); return; }

            var pd = new PsikologDegerlendirme
            {
                MahkumId = m.Id, DegerlendirmeTarihi = Tarih, RuhHali = RuhHali,
                AgresyonDuzeyi = Agresyon, KendineZararRiski = KendineZarar, Isbirligi = Isbirligi,
                OncekiDurumaGore = OncekiDurum, Aciklama = Aciklama
            };
            VeriDepolamaServisi.Instance.PsikologKaydet(pd);
            MesajGoster($"{MahkumKodu} için psikolog değerlendirmesi kaydedildi.", false);
            Temizle();
            MahkumKoduDegisti();
        }

        private void Guncelle()
        {
            if (_seciliKayit == null) return;
            _seciliKayit.DegerlendirmeTarihi = Tarih;
            _seciliKayit.RuhHali = RuhHali;
            _seciliKayit.AgresyonDuzeyi = Agresyon;
            _seciliKayit.KendineZararRiski = KendineZarar;
            _seciliKayit.Isbirligi = Isbirligi;
            _seciliKayit.OncekiDurumaGore = OncekiDurum;
            _seciliKayit.Aciklama = Aciklama;
            VeriDepolamaServisi.Instance.PsikologKaydet(_seciliKayit);
            MesajGoster("Kayıt güncellendi.", false);
            Temizle();
            MahkumKoduDegisti();
        }

        private void Sil()
        {
            if (_seciliKayit == null) return;
            VeriDepolamaServisi.Instance.PsikologSoftDelete(_seciliKayit.KayitId);
            MesajGoster("Kayıt pasife alındı.", false);
            Temizle();
            MahkumKoduDegisti();
        }

        private void KayitSec(object? p)
        {
            if (p is not PsikologDegerlendirme k) return;
            _seciliKayit = k;
            DuzenleModu = true;
            Tarih = k.DegerlendirmeTarihi;
            RuhHali = k.RuhHali;
            Agresyon = k.AgresyonDuzeyi;
            KendineZarar = k.KendineZararRiski;
            Isbirligi = k.Isbirligi;
            OncekiDurum = k.OncekiDurumaGore;
            Aciklama = k.Aciklama;
        }

        private void Temizle()
        {
            _seciliKayit = null;
            DuzenleModu = false;
            Tarih = DateTime.Today; RuhHali = ""; Agresyon = 1; KendineZarar = 1; Isbirligi = 3; OncekiDurum = "Aynı"; Aciklama = "";
        }

        private void MesajGoster(string msg, bool hata) { Mesaj = msg; MesajHata = hata; }
    }
}
