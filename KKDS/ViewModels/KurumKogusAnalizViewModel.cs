using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public sealed class KurumKogusAnalizViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;
        private KurumGenelAnalizOzet _genel = new();
        private bool _yukleniyor;
        private string _sonYenileme = "";

        public KurumGenelAnalizOzet Genel { get => _genel; set => SetProperty(ref _genel, value); }
        public bool Yukleniyor { get => _yukleniyor; set => SetProperty(ref _yukleniyor, value); }
        public string SonYenileme { get => _sonYenileme; set => SetProperty(ref _sonYenileme, value); }

        public ObservableCollection<BlokAnalizSatir> BlokOzet { get; } = new();
        public ObservableCollection<KogusAnalizSatir> KogusAnaliz { get; } = new();

        public ICommand YenileCommand { get; }
        public ICommand DetayCommand { get; }

        public KurumKogusAnalizViewModel(MainViewModel main)
        {
            _main = main;
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            YenileCommand = new RelayCommand(Yukle);
            DetayCommand = new RelayCommand(p =>
            {
                if (p is int id && id > 0)
                    _main.MahkumDetayGoster(id);
            });
            if (!YetkisizMod) Yukle();
        }

        private void Yukle()
        {
            Yukleniyor = true;
            try
            {
                var veri = VeriDepolamaServisi.Instance;
                veri.TumVerileriYukle();
                var risk = RiskServisi.Instance;
                var mahkumlar = veri.MahkumlariGetir(demoDahil: true, pasifDahil: false).ToList();
                var riskler = new Dictionary<int, RiskSkoru>();
                foreach (var m in mahkumlar)
                    riskler[m.Id] = risk.RiskSkoruHesapla(m.Id);

                var (genel, koguslar, bloklar) = KurumKogusAnalizServisi.Hesapla(veri, riskler);

                Genel = genel;
                BlokOzet.Clear();
                foreach (var b in bloklar) BlokOzet.Add(b);
                KogusAnaliz.Clear();
                foreach (var k in koguslar) KogusAnaliz.Add(k);

                SonYenileme = $"Son güncelleme: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            }
            finally
            {
                Yukleniyor = false;
            }
        }
    }
}
