using System;
using System.IO;
using System.Windows.Input;
using KKDS.Data;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class VeriAyarlariViewModel : BaseViewModel
    {
        private string _yol = "";
        private string _mesaj = "";
        private bool _mesajHata;

        public string VeriYoluGosterim
        {
            get => _yol;
            set => SetProperty(ref _yol, value);
        }

        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }

        public ICommand KaydetCommand { get; }
        public ICommand VarsayilanCommand { get; }

        public VeriAyarlariViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet);
            VarsayilanCommand = new RelayCommand(() =>
            {
                VeriYoluGosterim = UygulamaAyarlariServisi.VarsayilanVeriYolu();
            });
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            if (YetkisizMod) return;

            VeriYoluGosterim = string.IsNullOrWhiteSpace(UygulamaAyarlariServisi.Instance.Ayarlar.VeriKlasoruYolu)
                ? UygulamaAyarlariServisi.VarsayilanVeriYolu()
                : UygulamaAyarlariServisi.Instance.Ayarlar.VeriKlasoruYolu;
        }

        private void Kaydet()
        {
            if (string.IsNullOrWhiteSpace(VeriYoluGosterim))
            {
                Msg("Veri klasörü boş olamaz.", true);
                return;
            }

            try
            {
                var tam = Path.GetFullPath(VeriYoluGosterim.Trim());
                Directory.CreateDirectory(tam);
                UygulamaAyarlariServisi.Instance.VeriYolunuAyarla(tam);
                JsonDataRepository.Instance.VeriYolunuYenile();
                LogServisi.Instance.KritikIslem(LogIslemTipleri.AyarDegisiklik, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "ayar", null, $"Veri klasörü güncellendi: {tam}");
                Msg("Ayar kaydedildi. Veri yeniden yüklendi.", false);
            }
            catch (Exception ex)
            {
                Msg("Klasör oluşturulamadı veya yol geçersiz: " + ex.Message, true);
            }
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
