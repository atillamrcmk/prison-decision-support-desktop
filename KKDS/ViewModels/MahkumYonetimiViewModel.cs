using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class MahkumYonetimiViewModel : BaseViewModel
    {
        private string _mahkumKodu = "";
        private string _adSoyad = "";
        private string _blok = "A-Blok";
        private string _kogus = "";
        private DateTime _girisTarihi = DateTime.Today;
        private string _durum = "aktif";
        private string _not = "";
        private string _mesaj = "";
        private bool _mesajHata;
        private bool _duzenleModu;
        private Mahkum? _secili;
        private string _aramaMetni = "";

        public string MahkumKodu { get => _mahkumKodu; set => SetProperty(ref _mahkumKodu, value); }
        public string AdSoyad { get => _adSoyad; set => SetProperty(ref _adSoyad, value); }
        public string Blok { get => _blok; set => SetProperty(ref _blok, value); }
        public string Kogus { get => _kogus; set => SetProperty(ref _kogus, value); }
        public DateTime GirisTarihi { get => _girisTarihi; set => SetProperty(ref _girisTarihi, value); }
        public string Durum { get => _durum; set => SetProperty(ref _durum, value); }
        public string Not { get => _not; set => SetProperty(ref _not, value); }
        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }
        public bool DuzenleModu { get => _duzenleModu; set { SetProperty(ref _duzenleModu, value); OnPropertyChanged(nameof(FormBaslik)); } }
        public string FormBaslik => DuzenleModu ? "Mahkum Düzenleniyor" : "Yeni Mahkum";
        public string AramaMetni { get => _aramaMetni; set { SetProperty(ref _aramaMetni, value); ListeYenile(); } }

        public ObservableCollection<string> BlokListesi { get; } = new(Bloklar.Tumunu);
        public ObservableCollection<string> DurumListesi { get; } = new(MahkumDurumlari.Tumunu);
        public ObservableCollection<Mahkum> MahkumListesi { get; } = new();

        public ICommand KaydetCommand { get; }
        public ICommand GuncelleCommand { get; }
        public ICommand TemizleCommand { get; }
        public ICommand PasifYapCommand { get; }
        public ICommand KayitSecCommand { get; }
        public ICommand TestYukleCommand { get; }
        public ICommand TestTemizleCommand { get; }

        public MahkumYonetimiViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet);
            GuncelleCommand = new RelayCommand(Guncelle, () => DuzenleModu);
            TemizleCommand = new RelayCommand(Temizle);
            PasifYapCommand = new RelayCommand(PasifYap, () => DuzenleModu);
            KayitSecCommand = new RelayCommand(p => KayitSec(p));
            TestYukleCommand = new RelayCommand(TestYukle);
            TestTemizleCommand = new RelayCommand(TestTemizle);
            ListeYenile();
        }

        private void ListeYenile()
        {
            MahkumListesi.Clear();
            var list = string.IsNullOrWhiteSpace(AramaMetni)
                ? VeriDepolamaServisi.Instance.Mahkumlar.ToList()
                : VeriDepolamaServisi.Instance.MahkumAra(AramaMetni);
            foreach (var m in list) MahkumListesi.Add(m);
        }

        private void Kaydet()
        {
            if (string.IsNullOrWhiteSpace(AdSoyad)) { Msg("Ad Soyad gereklidir.", true); return; }
            var m = new Mahkum
            {
                MahkumKodu = MahkumKodu, AdSoyad = AdSoyad, Blok = Blok, Kogus = Kogus,
                KurumaGirisTarihi = GirisTarihi, Durum = Durum, Not = Not
            };
            VeriDepolamaServisi.Instance.MahkumKaydet(m);
            Msg($"{m.MahkumKodu} kaydedildi.", false);
            Temizle(); ListeYenile();
        }

        private void Guncelle()
        {
            if (_secili == null) return;
            _secili.MahkumKodu = MahkumKodu; _secili.AdSoyad = AdSoyad; _secili.Blok = Blok;
            _secili.Kogus = Kogus; _secili.KurumaGirisTarihi = GirisTarihi; _secili.Durum = Durum; _secili.Not = Not;
            VeriDepolamaServisi.Instance.MahkumKaydet(_secili);
            Msg("Mahkum güncellendi.", false); Temizle(); ListeYenile();
        }

        private void PasifYap()
        {
            if (_secili == null) return;
            VeriDepolamaServisi.Instance.MahkumSoftDelete(_secili.Id);
            Msg("Mahkum pasife alındı.", false); Temizle(); ListeYenile();
        }

        private void KayitSec(object? p)
        {
            if (p is not Mahkum m) return;
            _secili = m; DuzenleModu = true;
            MahkumKodu = m.MahkumKodu; AdSoyad = m.AdSoyad; Blok = m.Blok; Kogus = m.Kogus;
            GirisTarihi = m.KurumaGirisTarihi; Durum = m.Durum; Not = m.Not;
        }

        private void Temizle()
        {
            _secili = null; DuzenleModu = false;
            MahkumKodu = ""; AdSoyad = ""; Blok = "A-Blok"; Kogus = ""; GirisTarihi = DateTime.Today; Durum = "aktif"; Not = "";
        }

        private void TestYukle()
        {
            TestVeriOlusturucu.Olustur();
            VeriDepolamaServisi.Instance.TumVerileriYukle();
            Msg("Test verisi yüklendi.", false); ListeYenile();
        }

        private void TestTemizle()
        {
            VeriDepolamaServisi.Instance.TestVerisiniTemizle();
            Msg("Tüm veriler temizlendi.", false); ListeYenile();
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
