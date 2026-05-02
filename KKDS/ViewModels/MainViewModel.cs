using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class MenuItem
    {
        public string Baslik { get; set; } = "";
        public string Ikon { get; set; } = "";
        public ICommand Komut { get; set; } = null!;
    }

    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentView = null!;
        private Kullanici? _aktifKullanici;
        private bool _girisYapildi;
        private string _sayfaBasligi = "KKDS — Giriş";
        private string _altBilgi = "";

        public BaseViewModel CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        public Kullanici? AktifKullanici { get => _aktifKullanici; set { SetProperty(ref _aktifKullanici, value); OnPropertyChanged(nameof(KullaniciAdi)); OnPropertyChanged(nameof(RolAdi)); } }
        public bool GirisYapildi { get => _girisYapildi; set => SetProperty(ref _girisYapildi, value); }
        public string SayfaBasligi { get => _sayfaBasligi; set => SetProperty(ref _sayfaBasligi, value); }
        public string AltBilgi { get => _altBilgi; set => SetProperty(ref _altBilgi, value); }
        public string KullaniciAdi => AktifKullanici?.AdSoyad ?? "";
        public string RolAdi => AktifKullanici != null ? Roller.RolAdi(AktifKullanici.Rol) : "";
        public ObservableCollection<MenuItem> MenuItems { get; } = new();
        public ICommand CikisCommand { get; }

        public MainViewModel()
        {
            CikisCommand = new RelayCommand(CikisYap);
            GirisEkraniGoster();
        }

        private void GirisEkraniGoster()
        {
            var vm = new LoginViewModel();
            vm.GirisBasarili += OnGirisBasarili;
            CurrentView = vm;
        }

        private void OnGirisBasarili(Kullanici k)
        {
            AktifKullanici = k;
            OturumBilgisi.Instance.AktifKullanici = k;
            GirisYapildi = true;
            LogServisi.Instance.Giris(k.KullaniciAdi, k.Rol);

            VeriDepolamaServisi.Instance.TumVerileriYukle();

            MenuOlustur(k.Rol);

            switch (k.Rol)
            {
                case Roller.Psikolog: Navigate<PsikologFormViewModel>("Yeni Değerlendirme"); break;
                case Roller.Revir: Navigate<RevirFormViewModel>("Yeni Revir Kaydı"); break;
                case Roller.Disiplin: Navigate<DisiplinFormViewModel>("Yeni Olay Kaydı"); break;
                case Roller.Yonetici: DashboardGit(); break;
            }
        }

        private void MenuOlustur(string rol)
        {
            MenuItems.Clear();
            switch (rol)
            {
                case Roller.Psikolog:
                    MenuItems.Add(M("Yeni Değerlendirme", () => Navigate<PsikologFormViewModel>("Yeni Değerlendirme")));
                    MenuItems.Add(M("Geçmiş Değerlendirmeler", () => Navigate<PsikologGecmisViewModel>("Geçmiş Değerlendirmeler")));
                    MenuItems.Add(M("Kurul Kararları", () => Navigate<KurulOnaylarViewModel>("Kurul Kararları")));
                    MenuItems.Add(M("Mahkum Ara", () => Navigate<MahkumAraViewModel>("Mahkum Ara")));
                    break;
                case Roller.Revir:
                    MenuItems.Add(M("Yeni Revir Kaydı", () => Navigate<RevirFormViewModel>("Yeni Revir Kaydı")));
                    MenuItems.Add(M("112 Acil Yardım Kayıtları", () => Navigate<Acil112KayitViewModel>("112 Acil Yardım Kayıtları")));
                    MenuItems.Add(M("Geçmiş Revir Kayıtları", () => Navigate<RevirGecmisViewModel>("Geçmiş Revir Kayıtları")));
                    MenuItems.Add(M("Kurul Kararları", () => Navigate<KurulOnaylarViewModel>("Kurul Kararları")));
                    MenuItems.Add(M("Mahkum Ara", () => Navigate<MahkumAraViewModel>("Mahkum Ara")));
                    break;
                case Roller.Disiplin:
                    MenuItems.Add(M("Yeni Olay Kaydı", () => Navigate<DisiplinFormViewModel>("Yeni Olay Kaydı")));
                    MenuItems.Add(M("112 Acil Yardım Kayıtları", () => Navigate<Acil112KayitViewModel>("112 Acil Yardım Kayıtları")));
                    MenuItems.Add(M("Olay Kayıtları", () => Navigate<DisiplinGecmisViewModel>("Olay Kayıtları")));
                    MenuItems.Add(M("Kurul Kararları", () => Navigate<KurulOnaylarViewModel>("Kurul Kararları")));
                    MenuItems.Add(M("Mahkum Ara", () => Navigate<MahkumAraViewModel>("Mahkum Ara")));
                    break;
                case Roller.Yonetici:
                    MenuItems.Add(M("Gösterge Paneli", DashboardGit));
                    MenuItems.Add(M("Kurum ve Koğuş Analizi", () => Navigate(() => new KurumKogusAnalizViewModel(this), "Kurum ve Koğuş Analizi")));
                    MenuItems.Add(M("112 Acil Yardım Kayıtları", () => Navigate<Acil112KayitViewModel>("112 Acil Yardım Kayıtları")));
                    MenuItems.Add(M("Mahkum Listesi", () => Navigate(() => new MahkumListeViewModel(this), "Mahkum Listesi")));
                    MenuItems.Add(M("Kurul Değerlendirme", () => Navigate(() => new KurulViewModel(this), "Kurul Değerlendirme")));
                    MenuItems.Add(M("Yeni Kurul Kararı", () => Navigate<KurulKarariFormViewModel>("Yeni Kurul Kararı")));
                    MenuItems.Add(M("Karar Onay Takibi", () => Navigate<KurulOnaylarViewModel>("Karar Onay Takibi")));
                    MenuItems.Add(M("Tüm Kayıtlar", () => Navigate<TumKayitlarViewModel>("Tüm Kayıtlar")));
                    MenuItems.Add(M("Mahkum Yönetimi", () => Navigate<MahkumYonetimiViewModel>("Mahkum Yönetimi")));
                    MenuItems.Add(M("Kullanıcı Yönetimi", () => Navigate<KullaniciYonetimiViewModel>("Kullanıcı Yönetimi")));
                    MenuItems.Add(M("Veri Klasörü Ayarı", () => Navigate<VeriAyarlariViewModel>("Veri Klasörü Ayarı")));
                    MenuItems.Add(M("Sistem Günlükleri", () => Navigate<SistemLoglariViewModel>("Sistem Günlükleri")));
                    break;
            }
        }

        private static MenuItem M(string baslik, Action a) => new()
        {
            Baslik = baslik,
            Ikon = "",
            Komut = new RelayCommand(a)
        };

        private void Navigate<T>(string baslik) where T : BaseViewModel, new()
        {
            CurrentView = new T();
            SayfaBasligi = baslik;
            AltBilgi = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";
        }

        private void Navigate(Func<BaseViewModel> factory, string baslik)
        {
            CurrentView = factory();
            SayfaBasligi = baslik;
            AltBilgi = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";
        }

        private void DashboardGit()
        {
            CurrentView = new DashboardViewModel(this);
            SayfaBasligi = "Gösterge Paneli";
            AltBilgi = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";
        }

        public void MahkumDetayGoster(int mahkumId)
        {
            CurrentView = new MahkumDetayViewModel(mahkumId, this);
            SayfaBasligi = "Mahkum Detay";
            AltBilgi = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";
        }

        public void MahkumListeGoster()
        {
            Navigate(() => new MahkumListeViewModel(this), "Mahkum Listesi");
        }

        private void CikisYap()
        {
            if (AktifKullanici != null)
                LogServisi.Instance.Cikis(AktifKullanici.KullaniciAdi, AktifKullanici.Rol);
            AktifKullanici = null;
            OturumBilgisi.Instance.AktifKullanici = null;
            GirisYapildi = false;
            MenuItems.Clear();
            GirisEkraniGoster();
            SayfaBasligi = "KKDS — Giriş";
        }
    }
}
