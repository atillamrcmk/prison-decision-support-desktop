using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class SistemLoglariViewModel : BaseViewModel
    {
        private string _icerik = "";

        public string Icerik
        {
            get => _icerik;
            set => SetProperty(ref _icerik, value);
        }

        public ICommand YenileCommand { get; }

        public SistemLoglariViewModel()
        {
            YenileCommand = new RelayCommand(Yenile);
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            if (!YetkisizMod) Yenile();
        }

        private void Yenile()
        {
            var audit = LogServisi.Instance.SonAuditSatirlari(800);
            var eski = LogServisi.Instance.EskiMetinLoglariOku();
            Icerik = string.IsNullOrEmpty(audit) && string.IsNullOrEmpty(eski)
                ? "Henüz günlük kaydı yok."
                : "— Denetim günlüğü (audit) —\n" + audit + "\n\n— Eski metin günlükleri —\n" + eski;
        }
    }
}
