using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class MahkumListeViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;
        private string _aramaMetni = string.Empty;
        private string _seciliBlok = "Tümü";
        private string _seciliDurum = "Tümü";

        public string AramaMetni
        {
            get => _aramaMetni;
            set { SetProperty(ref _aramaMetni, value); Filtrele(); }
        }

        public string SeciliBlok
        {
            get => _seciliBlok;
            set { SetProperty(ref _seciliBlok, value); Filtrele(); }
        }

        public string SeciliDurum
        {
            get => _seciliDurum;
            set { SetProperty(ref _seciliDurum, value); Filtrele(); }
        }

        public ObservableCollection<string> BlokListesi { get; } = new() { "Tümü", "A-Blok", "B-Blok", "C-Blok", "D-Blok" };
        public ObservableCollection<string> DurumListesi { get; } = new() { "Tümü", "aktif", "yakin_izlem", "tahliye_edildi", "nakil" };
        public ObservableCollection<MahkumListeSatir> Mahkumlar { get; } = new();

        public ICommand DetayCommand { get; }

        public MahkumListeViewModel(MainViewModel main)
        {
            _main = main;
            DetayCommand = new RelayCommand(p => DetayGoster(p));
            Filtrele();
        }

        private void Filtrele()
        {
            Mahkumlar.Clear();
            var veri = VeriDepolamaServisi.Instance;
            var analiz = AnalizServisi.Instance;
            var risk = RiskServisi.Instance;

            var query = veri.Mahkumlar.AsEnumerable();

            if (SeciliBlok != "Tümü")
                query = query.Where(m => m.Blok == SeciliBlok);

            if (SeciliDurum != "Tümü")
                query = query.Where(m => m.Durum == SeciliDurum);

            if (!string.IsNullOrWhiteSpace(AramaMetni))
                query = query.Where(m => m.MahkumKodu.Contains(AramaMetni, StringComparison.OrdinalIgnoreCase));

            foreach (var m in query)
            {
                var a = analiz.TamAnaliz(m.Id);
                var r = risk.RiskSkoruHesapla(m.Id);
                var olaylar = veri.MahkumOlaylari(m.Id);
                var kararlar = veri.MahkumKararlari(m.Id);

                var sonOlay = olaylar.FirstOrDefault();
                var sonKarar = kararlar.FirstOrDefault();

                Mahkumlar.Add(new MahkumListeSatir
                {
                    MahkumId = m.Id,
                    MahkumKodu = m.MahkumKodu,
                    Blok = m.Blok,
                    Kogus = m.Kogus,
                    Durum = DurumGoster(m.Durum),
                    SonOlayTarihi = sonOlay?.OlayTarihi.ToString("dd.MM.yyyy") ?? "-",
                    SonKurulKarari = sonKarar?.KararTuru ?? "-",
                    RiskSkoru = r.ToplamSkor,
                    RiskSeviye = r.Seviye,
                    Egilim = a.Egilim
                });
            }
        }

        private string DurumGoster(string durum) => durum switch
        {
            "aktif" => "Aktif",
            "yakin_izlem" => "Yakın İzlem",
            "tahliye_edildi" => "Tahliye",
            "nakil" => "Nakil",
            _ => durum
        };

        private void DetayGoster(object? param)
        {
            if (param is int id)
                _main.MahkumDetayGoster(id);
        }
    }

    public class MahkumListeSatir
    {
        public int MahkumId { get; set; }
        public string MahkumKodu { get; set; } = "";
        public string Blok { get; set; } = "";
        public string Kogus { get; set; } = "";
        public string Durum { get; set; } = "";
        public string SonOlayTarihi { get; set; } = "";
        public string SonKurulKarari { get; set; } = "";
        public double RiskSkoru { get; set; }
        public string RiskSeviye { get; set; } = "";
        public string Egilim { get; set; } = "";
    }
}
