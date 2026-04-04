using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class KullaniciSatir
    {
        public string KullaniciAdi { get; set; } = "";
        public string AdSoyad { get; set; } = "";
        public string Rol { get; set; } = "";
        public string RolAdi { get; set; } = "";
    }

    public class KullaniciYonetimiViewModel : BaseViewModel
    {
        private string _kullaniciAdi = "";
        private string _sifre = "";
        private string _sifreTekrar = "";
        private string _adSoyad = "";
        private string _seciliRol = Roller.Psikolog;
        private string _mesaj = "";
        private bool _mesajHata;
        private bool _duzenleModu;
        private string? _duzenlenecekKullaniciAdi;

        public string KullaniciAdi { get => _kullaniciAdi; set => SetProperty(ref _kullaniciAdi, value); }
        public string Sifre { get => _sifre; set => SetProperty(ref _sifre, value); }
        public string SifreTekrar { get => _sifreTekrar; set => SetProperty(ref _sifreTekrar, value); }
        public string AdSoyad { get => _adSoyad; set => SetProperty(ref _adSoyad, value); }
        public string SeciliRol { get => _seciliRol; set => SetProperty(ref _seciliRol, value); }
        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }
        public bool DuzenleModu { get => _duzenleModu; set { SetProperty(ref _duzenleModu, value); OnPropertyChanged(nameof(FormBaslik)); OnPropertyChanged(nameof(KullaniciAdiDuzenlenebilir)); } }
        public string FormBaslik => DuzenleModu ? "Kullanıcı Düzenleniyor" : "Yeni Kullanıcı Oluştur";
        public bool KullaniciAdiDuzenlenebilir => !DuzenleModu;

        public ObservableCollection<string> RolListesi { get; } = new(Roller.Tumunu);
        public ObservableCollection<KullaniciSatir> Kullanicilar { get; } = new();

        public ICommand KaydetCommand { get; }
        public ICommand GuncelleCommand { get; }
        public ICommand TemizleCommand { get; }
        public ICommand SilCommand { get; }
        public ICommand SecCommand { get; }

        public KullaniciYonetimiViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet);
            GuncelleCommand = new RelayCommand(Guncelle, () => DuzenleModu);
            TemizleCommand = new RelayCommand(Temizle);
            SilCommand = new RelayCommand(Sil, () => DuzenleModu);
            SecCommand = new RelayCommand(p => Sec(p));
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            if (!YetkisizMod) ListeYenile();
        }

        private void ListeYenile()
        {
            Kullanicilar.Clear();
            foreach (var k in VeriDepolamaServisi.Instance.KullanicilarListesi.OrderBy(x => x.Rol).ThenBy(x => x.KullaniciAdi))
            {
                Kullanicilar.Add(new KullaniciSatir
                {
                    KullaniciAdi = k.KullaniciAdi,
                    AdSoyad = k.AdSoyad,
                    Rol = k.Rol,
                    RolAdi = Roller.RolAdi(k.Rol)
                });
            }
        }

        private void Kaydet()
        {
            if (string.IsNullOrWhiteSpace(KullaniciAdi))
            { Msg("Kullanıcı adı gereklidir.", true); return; }
            if (string.IsNullOrWhiteSpace(AdSoyad))
            { Msg("Ad Soyad gereklidir.", true); return; }
            if (string.IsNullOrWhiteSpace(Sifre))
            { Msg("Şifre gereklidir.", true); return; }
            if (Sifre != SifreTekrar)
            { Msg("Şifreler eşleşmiyor.", true); return; }
            if (KullaniciAdi.Contains(' '))
            { Msg("Kullanıcı adında boşluk kullanılamaz.", true); return; }

            if (VeriDepolamaServisi.Instance.KullaniciVarMi(KullaniciAdi))
            { Msg($"'{KullaniciAdi}' kullanıcı adı zaten mevcut.", true); return; }

            var yeni = new Kullanici
            {
                KullaniciAdi = KullaniciAdi.Trim().ToLower(),
                Sifre = Sifre,
                AdSoyad = AdSoyad.Trim(),
                Rol = SeciliRol
            };
            VeriDepolamaServisi.Instance.KullaniciKaydet(yeni);
            Msg($"'{yeni.KullaniciAdi}' kullanıcısı ({Roller.RolAdi(yeni.Rol)}) oluşturuldu.", false);
            Temizle();
            ListeYenile();
        }

        private void Guncelle()
        {
            if (_duzenlenecekKullaniciAdi == null) return;
            if (string.IsNullOrWhiteSpace(AdSoyad))
            { Msg("Ad Soyad gereklidir.", true); return; }

            var mevcut = VeriDepolamaServisi.Instance.KullanicilarListesi
                .FirstOrDefault(k => k.KullaniciAdi.Equals(_duzenlenecekKullaniciAdi, StringComparison.OrdinalIgnoreCase));
            if (mevcut == null) return;

            mevcut.AdSoyad = AdSoyad.Trim();
            mevcut.Rol = SeciliRol;
            if (!string.IsNullOrWhiteSpace(Sifre))
            {
                if (Sifre != SifreTekrar) { Msg("Şifreler eşleşmiyor.", true); return; }
                mevcut.Sifre = Sifre;
            }

            VeriDepolamaServisi.Instance.KullaniciKaydet(mevcut);
            Msg($"'{mevcut.KullaniciAdi}' kullanıcısı güncellendi.", false);
            Temizle();
            ListeYenile();
        }

        private void Sil()
        {
            if (_duzenlenecekKullaniciAdi == null) return;

            var oturum = OturumBilgisi.Instance;
            if (_duzenlenecekKullaniciAdi.Equals(oturum.KullaniciAdi, StringComparison.OrdinalIgnoreCase))
            { Msg("Kendi hesabınızı silemezsiniz.", true); return; }

            VeriDepolamaServisi.Instance.KullaniciSil(_duzenlenecekKullaniciAdi);
            Msg($"'{_duzenlenecekKullaniciAdi}' kullanıcısı silindi.", false);
            Temizle();
            ListeYenile();
        }

        private void Sec(object? p)
        {
            if (p is not KullaniciSatir s) return;
            _duzenlenecekKullaniciAdi = s.KullaniciAdi;
            DuzenleModu = true;
            KullaniciAdi = s.KullaniciAdi;
            AdSoyad = s.AdSoyad;
            SeciliRol = s.Rol;
            Sifre = "";
            SifreTekrar = "";
        }

        private void Temizle()
        {
            _duzenlenecekKullaniciAdi = null;
            DuzenleModu = false;
            KullaniciAdi = ""; AdSoyad = ""; Sifre = ""; SifreTekrar = ""; SeciliRol = Roller.Psikolog;
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
