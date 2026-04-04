using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KKDS.Helpers
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _yetkisizMod;
        private string _yetkisizAciklama = "";

        /// <summary>Rol kontrolü başarısız olduğunda true; ilgili View boş/uyarı gösterir.</summary>
        public bool YetkisizMod
        {
            get => _yetkisizMod;
            set => SetProperty(ref _yetkisizMod, value);
        }

        public string YetkisizAciklama
        {
            get => _yetkisizAciklama;
            set => SetProperty(ref _yetkisizAciklama, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
