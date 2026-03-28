using System;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _kullaniciAdi = string.Empty;
        private string _sifre = string.Empty;
        private string _hataMesaji = string.Empty;

        public string KullaniciAdi { get => _kullaniciAdi; set { SetProperty(ref _kullaniciAdi, value); HataMesaji = ""; } }
        public string Sifre { get => _sifre; set { SetProperty(ref _sifre, value); HataMesaji = ""; } }
        public string HataMesaji { get => _hataMesaji; set => SetProperty(ref _hataMesaji, value); }

        public ICommand GirisCommand { get; }
        public event Action<Kullanici>? GirisBasarili;

        public LoginViewModel()
        {
            GirisCommand = new RelayCommand(GirisYap);
        }

        private void GirisYap()
        {
            if (string.IsNullOrWhiteSpace(KullaniciAdi) || string.IsNullOrWhiteSpace(Sifre))
            { HataMesaji = "Kullanıcı adı ve şifre gereklidir."; return; }

            var k = VeriDepolamaServisi.Instance.GirisYap(KullaniciAdi, Sifre);
            if (k != null) GirisBasarili?.Invoke(k);
            else HataMesaji = "Geçersiz kullanıcı adı veya şifre.";
        }
    }
}
