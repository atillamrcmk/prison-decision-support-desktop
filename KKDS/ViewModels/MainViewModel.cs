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

            TestVeriOlusturucu.Olustur();
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
                    MenuItems.Add(new MenuItem { Baslik = "Yeni Değerlendirme", Ikon = "📝", Komut = new RelayCommand(() => Navigate<PsikologFormViewModel>("Yeni Değerlendirme")) });
                    MenuItems.Add(new MenuItem { Baslik = "Geçmiş Değerlendirmeler", Ikon = "📋", Komut = new RelayCommand(() => Navigate<PsikologGecmisViewModel>("Geçmiş Değerlendirmeler")) });
                    MenuItems.Add(new MenuItem { Baslik = "Kurul Kararları", Ikon = "⚖️", Komut = new RelayCommand(() => Navigate<KurulOnaylarViewModel>("Kurul Kararları")) });
                    MenuItems.Add(new MenuItem { Baslik = "Mahkum Ara", Ikon = "🔍", Komut = new RelayCommand(() => Navigate<MahkumAraViewModel>("Mahkum Ara")) });
                    break;
                case Roller.Revir:
                    MenuItems.Add(new MenuItem { Baslik = "Yeni Revir Kaydı", Ikon = "📝", Komut = new RelayCommand(() => Navigate<RevirFormViewModel>("Yeni Revir Kaydı")) });
                    MenuItems.Add(new MenuItem { Baslik = "Geçmiş Revir Kayıtları", Ikon = "📋", Komut = new RelayCommand(() => Navigate<RevirGecmisViewModel>("Geçmiş Revir Kayıtları")) });
                    MenuItems.Add(new MenuItem { Baslik = "Kurul Kararları", Ikon = "⚖️", Komut = new RelayCommand(() => Navigate<KurulOnaylarViewModel>("Kurul Kararları")) });
                    MenuItems.Add(new MenuItem { Baslik = "Mahkum Ara", Ikon = "🔍", Komut = new RelayCommand(() => Navigate<MahkumAraViewModel>("Mahkum Ara")) });
                    break;
                case Roller.Disiplin:
                    MenuItems.Add(new MenuItem { Baslik = "Yeni Olay Kaydı", Ikon = "📝", Komut = new RelayCommand(() => Navigate<DisiplinFormViewModel>("Yeni Olay Kaydı")) });
                    MenuItems.Add(new MenuItem { Baslik = "Olay Kayıtları", Ikon = "📋", Komut = new RelayCommand(() => Navigate<DisiplinGecmisViewModel>("Olay Kayıtları")) });
                    MenuItems.Add(new MenuItem { Baslik = "Kurul Kararları", Ikon = "⚖️", Komut = new RelayCommand(() => Navigate<KurulOnaylarViewModel>("Kurul Kararları")) });
                    MenuItems.Add(new MenuItem { Baslik = "Mahkum Ara", Ikon = "🔍", Komut = new RelayCommand(() => Navigate<MahkumAraViewModel>("Mahkum Ara")) });
                    break;
                case Roller.Yonetici:
                    MenuItems.Add(new MenuItem { Baslik = "Gösterge Paneli", Ikon = "📊", Komut = new RelayCommand(DashboardGit) });
                    MenuItems.Add(new MenuItem { Baslik = "Mahkum Listesi", Ikon = "👥", Komut = new RelayCommand(() => Navigate(() => new MahkumListeViewModel(this), "Mahkum Listesi")) });
                    MenuItems.Add(new MenuItem { Baslik = "Kurul Değerlendirme", Ikon = "⚖️", Komut = new RelayCommand(() => Navigate(() => new KurulViewModel(this), "Kurul Değerlendirme")) });
                    MenuItems.Add(new MenuItem { Baslik = "Yeni Kurul Kararı", Ikon = "📜", Komut = new RelayCommand(() => Navigate<KurulKarariFormViewModel>("Yeni Kurul Kararı")) });
                    MenuItems.Add(new MenuItem { Baslik = "Karar Onay Takibi", Ikon = "✅", Komut = new RelayCommand(() => Navigate<KurulOnaylarViewModel>("Karar Onay Takibi")) });
                    MenuItems.Add(new MenuItem { Baslik = "Tüm Kayıtlar", Ikon = "🗂️", Komut = new RelayCommand(() => Navigate<TumKayitlarViewModel>("Tüm Kayıtlar")) });
                    MenuItems.Add(new MenuItem { Baslik = "Mahkum Yönetimi", Ikon = "⚙️", Komut = new RelayCommand(() => Navigate<MahkumYonetimiViewModel>("Mahkum Yönetimi")) });
                    MenuItems.Add(new MenuItem { Baslik = "Kullanıcı Yönetimi", Ikon = "👤", Komut = new RelayCommand(() => Navigate<KullaniciYonetimiViewModel>("Kullanıcı Yönetimi")) });
                    break;
            }
        }

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
