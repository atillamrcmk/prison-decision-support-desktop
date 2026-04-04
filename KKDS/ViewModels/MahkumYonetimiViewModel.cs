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
        private bool _pasifGoster;
        private bool _demoGoster;

        public bool PasifKayitlariGoster { get => _pasifGoster; set { SetProperty(ref _pasifGoster, value); ListeYenile(); } }
        public bool DemoKayitlariGoster { get => _demoGoster; set { SetProperty(ref _demoGoster, value); ListeYenile(); } }

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
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            if (!YetkisizMod) ListeYenile();
        }

        private void ListeYenile()
        {
            MahkumListesi.Clear();
            var veri = VeriDepolamaServisi.Instance;
            List<Mahkum> list;
            if (string.IsNullOrWhiteSpace(AramaMetni))
                list = veri.MahkumlariGetir(DemoKayitlariGoster, PasifKayitlariGoster);
            else
            {
                list = veri.MahkumAra(AramaMetni);
                if (!PasifKayitlariGoster) list = list.Where(m => m.AktifMi).ToList();
                if (!DemoKayitlariGoster) list = list.Where(m => !m.IsDemoData).ToList();
            }
            foreach (var m in list) MahkumListesi.Add(m);
        }

        private void Kaydet()
        {
            var hata = FormDogrulama.Birlestir(
                FormDogrulama.TarihKontrol(GirisTarihi, "Kuruma giriş tarihi"),
                FormDogrulama.ComboZorunlu(Durum, "Durum"),
                FormDogrulama.ComboZorunlu(Blok, "Blok"),
                FormDogrulama.AciklamaUzunluk(Not));
            if (!string.IsNullOrEmpty(hata)) { Msg(hata, true); return; }
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
            var hata = FormDogrulama.Birlestir(
                FormDogrulama.TarihKontrol(GirisTarihi, "Kuruma giriş tarihi"),
                FormDogrulama.ComboZorunlu(Durum, "Durum"),
                FormDogrulama.ComboZorunlu(Blok, "Blok"),
                FormDogrulama.AciklamaUzunluk(Not));
            if (!string.IsNullOrEmpty(hata)) { Msg(hata, true); return; }
            if (string.IsNullOrWhiteSpace(AdSoyad)) { Msg("Ad Soyad gereklidir.", true); return; }
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
            VeriDepolamaServisi.Instance.DemoVerisiYukle();
            Msg("Demo verisi yüklendi.", false); ListeYenile();
        }

        private void TestTemizle()
        {
            VeriDepolamaServisi.Instance.DemoVerisiniTemizle();
            Msg("Demo kayıtları temizlendi.", false); ListeYenile();
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
