using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Data;
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
        private bool _pasifGoster;
        private bool _demoGoster;
        private bool _riskUstu70;
        private bool _son7GunOlay;
        private Dictionary<int, MahkumListeOzet>? _ozetCache;
        private int _gorunenKayitSayisi;

        public bool PasifKayitlariGoster { get => _pasifGoster; set { SetProperty(ref _pasifGoster, value); _ozetCache = null; Filtrele(); } }
        public bool DemoKayitlariGoster { get => _demoGoster; set { SetProperty(ref _demoGoster, value); _ozetCache = null; Filtrele(); } }
        public bool RiskUstu70Filtre { get => _riskUstu70; set { SetProperty(ref _riskUstu70, value); Filtrele(); } }
        public bool Son7GunOlayFiltre { get => _son7GunOlay; set { SetProperty(ref _son7GunOlay, value); Filtrele(); } }

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
        public int GorunenKayitSayisi { get => _gorunenKayitSayisi; set => SetProperty(ref _gorunenKayitSayisi, value); }

        public ICommand DetayCommand { get; }
        public ICommand ListeYenileCommand { get; }

        public MahkumListeViewModel(MainViewModel main)
        {
            _main = main;
            DetayCommand = new RelayCommand(p => DetayGoster(p));
            ListeYenileCommand = new RelayCommand(() => { _ozetCache = null; Filtrele(); });
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            if (!YetkisizMod) IlkYukle();
        }

        /// <summary>
        /// Diskten veriyi al; yalnızca demo mahkum varsa (üretim kaydı yok) listeyi demo dahil göster.
        /// Aksi halde demo varsayılan kapalı kalır.
        /// </summary>
        private void IlkYukle()
        {
            IDataRepository veri = VeriDepolamaServisi.Instance;
            veri.TumVerileriYukle();
            if (veri.DemoMahkumVarMi() && veri.Mahkumlar.Count == 0)
                DemoKayitlariGoster = true;
            else
                Filtrele();
        }

        private void OzetCacheDoldur()
        {
            var veri = VeriDepolamaServisi.Instance;
            veri.TumVerileriYukle();
            var analiz = AnalizServisi.Instance;
            var risk = RiskServisi.Instance;
            var bugun = DateTime.Today;
            _ozetCache = new Dictionary<int, MahkumListeOzet>();
            foreach (var m in veri.MahkumlariGetir(DemoKayitlariGoster, PasifKayitlariGoster))
            {
                var son7 = veri.MahkumOlaylari(m.Id).Count(o => o.OlayTarihi >= bugun.AddDays(-7));
                var rs = risk.RiskSkoruHesapla(m.Id);
                _ozetCache[m.Id] = new MahkumListeOzet
                {
                    Risk = rs.ToplamSkor,
                    Seviye = rs.Seviye,
                    Egilim = analiz.TamAnaliz(m.Id).Egilim,
                    Son7GunOlay = son7
                };
            }
        }

        private void Filtrele()
        {
            Mahkumlar.Clear();
            if (_ozetCache == null) OzetCacheDoldur();

            var veri = VeriDepolamaServisi.Instance;

            var query = veri.MahkumlariGetir(DemoKayitlariGoster, PasifKayitlariGoster).AsEnumerable();

            if (SeciliBlok != "Tümü")
                query = query.Where(m => m.Blok == SeciliBlok);

            if (SeciliDurum != "Tümü")
                query = query.Where(m => m.Durum == SeciliDurum);

            if (!string.IsNullOrWhiteSpace(AramaMetni))
                query = query.Where(m => m.MahkumKodu.Contains(AramaMetni, StringComparison.OrdinalIgnoreCase));

            foreach (var m in query)
            {
                var oz = _ozetCache![m.Id];
                if (RiskUstu70Filtre && oz.Risk <= 70) continue;
                if (Son7GunOlayFiltre && oz.Son7GunOlay == 0) continue;

                var olaylar = veri.MahkumOlaylari(m.Id);
                var kararlar = veri.MahkumKararlari(m.Id);
                var sonOlay = olaylar.OrderByDescending(o => o.OlayTarihi).FirstOrDefault();
                var sonKarar = kararlar.OrderByDescending(k => k.KararTarihi).FirstOrDefault();

                Mahkumlar.Add(new MahkumListeSatir
                {
                    MahkumId = m.Id,
                    MahkumKodu = m.MahkumKodu,
                    Blok = m.Blok,
                    Kogus = m.Kogus,
                    Durum = DurumGoster(m.Durum),
                    SonOlayTarihi = sonOlay?.OlayTarihi.ToString("dd.MM.yyyy") ?? "-",
                    SonKurulKarari = sonKarar?.KararTuru ?? "-",
                    RiskSkoru = oz.Risk,
                    RiskSeviye = oz.Seviye,
                    Egilim = oz.Egilim
                });
            }
            GorunenKayitSayisi = Mahkumlar.Count;
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

    internal sealed class MahkumListeOzet
    {
        public double Risk { get; set; }
        public string Seviye { get; set; } = "";
        public string Egilim { get; set; } = "";
        public int Son7GunOlay { get; set; }
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
