using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class MahkumAraViewModel : BaseViewModel
    {
        private string _aramaMetni = "";
        public string AramaMetni { get => _aramaMetni; set { SetProperty(ref _aramaMetni, value); Ara(); } }
        public ObservableCollection<Mahkum> Sonuclar { get; } = new();
        public ICommand AraCommand { get; }

        public MahkumAraViewModel()
        {
            AraCommand = new RelayCommand(Ara);
            YetkiServisi.ViewModelKoruma(this, Roller.Psikolog, Roller.Revir, Roller.Disiplin);
            if (!YetkisizMod)
                foreach (var m in VeriDepolamaServisi.Instance.MahkumlariGetir(demoDahil: true, pasifDahil: false))
                    Sonuclar.Add(m);
        }

        private void Ara()
        {
            Sonuclar.Clear();
            var list = string.IsNullOrWhiteSpace(AramaMetni)
                ? VeriDepolamaServisi.Instance.MahkumlariGetir(demoDahil: true, pasifDahil: false)
                : VeriDepolamaServisi.Instance.MahkumAra(AramaMetni);
            foreach (var m in list) Sonuclar.Add(m);
        }
    }
}
