using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class KararListeSatir
    {
        public string KayitId { get; set; } = "";
        public string MahkumKodu { get; set; } = "";
        public string MahkumAd { get; set; } = "";
        public string KararTuru { get; set; } = "";
        public DateTime KararTarihi { get; set; }
        public string KisaGerekce { get; set; } = "";
        public int SureGun { get; set; }
        public string OnayDurumuMetin { get; set; } = "";
        public string OnayDurumuRenk { get; set; } = "";
        public string OnayOzet { get; set; } = "";
    }

    public class KurulKarariFormViewModel : BaseViewModel
    {
        private string _mahkumKodu = "";
        private DateTime _tarih = DateTime.Today;
        private string _kararTuru = "";
        private int _sureGun = 7;
        private string _kisaGerekce = "";
        private string _detayliGerekce = "";
        private DateTime? _gozdenGecirmeTarihi;
        private string _mesaj = "";
        private bool _mesajHata;

        public string MahkumKodu { get => _mahkumKodu; set => SetProperty(ref _mahkumKodu, value); }
        public DateTime Tarih { get => _tarih; set => SetProperty(ref _tarih, value); }
        public string KararTuru { get => _kararTuru; set => SetProperty(ref _kararTuru, value); }
        public int SureGun { get => _sureGun; set => SetProperty(ref _sureGun, value); }
        public string KisaGerekce { get => _kisaGerekce; set => SetProperty(ref _kisaGerekce, value); }
        public string DetayliGerekce { get => _detayliGerekce; set => SetProperty(ref _detayliGerekce, value); }
        public DateTime? GozdenGecirmeTarihi { get => _gozdenGecirmeTarihi; set => SetProperty(ref _gozdenGecirmeTarihi, value); }
        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }

        public ObservableCollection<string> KararTurleriListesi { get; } = new(KararTurleri.Tumunu);
        public ObservableCollection<KararListeSatir> MevcutKararlar { get; } = new();

        public ICommand KaydetCommand { get; }
        public ICommand TemizleCommand { get; }

        public KurulKarariFormViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet);
            TemizleCommand = new RelayCommand(Temizle);
            ListeYenile();
        }

        private void Kaydet()
        {
            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) { Msg("Mahkum bulunamadı.", true); return; }
            if (string.IsNullOrEmpty(KararTuru)) { Msg("Karar türü seçilmelidir.", true); return; }
            if (string.IsNullOrWhiteSpace(KisaGerekce)) { Msg("Kısa gerekçe girilmelidir.", true); return; }

            var karar = new KurulKarari
            {
                MahkumId = m.Id,
                KararTarihi = Tarih,
                KararTuru = KararTuru,
                SureGun = SureGun,
                KisaGerekce = KisaGerekce,
                DetayliGerekce = DetayliGerekce,
                GozdenGecirmeTarihi = GozdenGecirmeTarihi,
                OnayDurumu = OnayDurumlari.Beklemede
            };
            karar.OnayBilgisiOlustur();
            VeriDepolamaServisi.Instance.KurulKarariKaydet(karar);

            Msg($"{MahkumKodu} için kurul kararı oluşturuldu. Psikolog, Revir ve Disiplin onayı bekleniyor.", false);
            Temizle();
            ListeYenile();
        }

        private void ListeYenile()
        {
            MevcutKararlar.Clear();
            var kararlar = VeriDepolamaServisi.Instance.TumKurulKararlari();
            foreach (var k in kararlar.Take(50))
            {
                var m = VeriDepolamaServisi.Instance.MahkumBulById(k.MahkumId);
                MevcutKararlar.Add(new KararListeSatir
                {
                    KayitId = k.KayitId,
                    MahkumKodu = k.MahkumKodu,
                    MahkumAd = m?.AdSoyad ?? "-",
                    KararTuru = k.KararTuru,
                    KararTarihi = k.KararTarihi,
                    KisaGerekce = k.KisaGerekce,
                    SureGun = k.SureGun,
                    OnayDurumuMetin = OnayDurumlari.Goster(k.OnayDurumu),
                    OnayDurumuRenk = OnayDurumlari.Renk(k.OnayDurumu),
                    OnayOzet = $"{k.OnaylayanSayisi}/{k.ToplamOnayGerekli} onay"
                });
            }
        }

        private void Temizle()
        {
            MahkumKodu = ""; Tarih = DateTime.Today; KararTuru = ""; SureGun = 7;
            KisaGerekce = ""; DetayliGerekce = ""; GozdenGecirmeTarihi = null;
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
