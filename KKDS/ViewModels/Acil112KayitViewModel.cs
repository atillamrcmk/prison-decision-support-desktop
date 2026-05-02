using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class Acil112KayitViewModel : BaseViewModel
    {
        private string _mahkumKodu = "";
        private DateTime _cagriTarih = DateTime.Today;
        private string _cagriSaat = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
        private string _vardiya = NobetVardiyalari.Tumunu[0];
        private string _cagiranVardiya = NobetVardiyalari.Tumunu[0];
        private string _sikayet = "";
        private string _notlar = "";
        private string _filtreMetni = "";
        private string _mesaj = "";
        private bool _mesajHata;
        private bool _duzenleModu;
        private Acil112CagriKaydi? _secili;

        public string MahkumKodu { get => _mahkumKodu; set => SetProperty(ref _mahkumKodu, value); }
        public DateTime CagriTarih { get => _cagriTarih; set => SetProperty(ref _cagriTarih, value); }
        public string CagriSaat { get => _cagriSaat; set => SetProperty(ref _cagriSaat, value); }
        public string Vardiya { get => _vardiya; set => SetProperty(ref _vardiya, value); }
        public string CagiranVardiya { get => _cagiranVardiya; set => SetProperty(ref _cagiranVardiya, value); }
        public string Sikayet { get => _sikayet; set => SetProperty(ref _sikayet, value); }
        public string Notlar { get => _notlar; set => SetProperty(ref _notlar, value); }
        public string FiltreMetni
        {
            get => _filtreMetni;
            set
            {
                if (SetProperty(ref _filtreMetni, value))
                    YenileListe();
            }
        }

        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }
        public bool DuzenleModu
        {
            get => _duzenleModu;
            set
            {
                if (SetProperty(ref _duzenleModu, value))
                    OnPropertyChanged(nameof(FormBaslik));
            }
        }

        public string FormBaslik => DuzenleModu ? "Kayıt Düzenleniyor" : "Yeni 112 Acil Yardım Çağrısı";

        public ObservableCollection<string> VardiyaListesi { get; } = new(NobetVardiyalari.Tumunu);
        public ObservableCollection<Acil112CagriKaydi> GorunenKayitlar { get; } = new();

        public ICommand KaydetCommand { get; }
        public ICommand GuncelleCommand { get; }
        public ICommand TemizleCommand { get; }
        public ICommand SilCommand { get; }
        public ICommand KayitSecCommand { get; }
        public ICommand ListeYenileCommand { get; }

        public Acil112KayitViewModel()
        {
            KaydetCommand = new RelayCommand(Kaydet, () => !YetkisizMod);
            GuncelleCommand = new RelayCommand(Guncelle, () => DuzenleModu && !YetkisizMod);
            TemizleCommand = new RelayCommand(Temizle);
            SilCommand = new RelayCommand(Sil, () => DuzenleModu && !YetkisizMod);
            KayitSecCommand = new RelayCommand(p => KayitSec(p));
            ListeYenileCommand = new RelayCommand(() => YenileListe());
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici, Roller.Revir, Roller.Disiplin);
            if (!YetkisizMod) YenileListe();
        }

        private void YenileListe()
        {
            GorunenKayitlar.Clear();
            var q = VeriDepolamaServisi.Instance.TumAcil112Kayitlari(demoDahil: false).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(FiltreMetni))
            {
                var f = FiltreMetni.Trim();
                q = q.Where(a =>
                    a.MahkumKodu.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    a.MahkumAdSoyad.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    a.Sikayet.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    a.Vardiya.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    a.CagiranVardiya.Contains(f, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var a in q)
                GorunenKayitlar.Add(a);
        }

        private static bool SaatCoz(string? s, out int saat, out int dakika)
        {
            saat = 0;
            dakika = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            var parts = s.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out saat)) return false;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dakika)) return false;
            if (saat is < 0 or > 23 || dakika is < 0 or > 59) return false;
            return true;
        }

        private bool CagriZamaniOlustur(out DateTime z, out string? hata)
        {
            z = default;
            hata = FormDogrulama.TarihKontrol(CagriTarih, "Çağrı tarihi");
            if (!string.IsNullOrEmpty(hata)) return false;
            if (!SaatCoz(CagriSaat, out int h, out int m))
            {
                hata = "Saat biçimi geçersiz (örn. 14:30).";
                return false;
            }

            z = CagriTarih.Date.Add(new TimeSpan(h, m, 0));
            if (z > DateTime.Now.AddMinutes(2))
            {
                hata = "Çağrı zamanı gelecekte olamaz.";
                return false;
            }

            hata = null;
            return true;
        }

        private static string? SikayetKontrol(string? s)
        {
            if (FormDogrulama.BosMu(s)) return "Şikâyet / başvuru nedeni zorunludur.";
            if (s!.Trim().Length < 4) return "Şikâyet metni en az 4 karakter olmalıdır.";
            return FormDogrulama.AciklamaUzunluk(s);
        }

        private void Kaydet()
        {
            var hata = FormDogrulama.Birlestir(
                FormDogrulama.MahkumKoduKontrol(MahkumKodu),
                FormDogrulama.ComboZorunlu(Vardiya, "Olayın vardiyası"),
                FormDogrulama.ComboZorunlu(CagiranVardiya, "112'yi arayan vardiya"),
                SikayetKontrol(Sikayet),
                FormDogrulama.AciklamaUzunluk(Notlar));
            if (!string.IsNullOrEmpty(hata)) { Msg(hata, true); return; }
            if (!CagriZamaniOlustur(out var cagriZamani, out var zHata))
            {
                Msg(zHata ?? "Zaman geçersiz.", true);
                return;
            }

            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) { Msg("Mahkum bulunamadı.", true); return; }

            var kayit = new Acil112CagriKaydi
            {
                MahkumId = m.Id,
                MahkumAdSoyad = m.AdSoyad,
                CagriZamani = cagriZamani,
                Vardiya = Vardiya,
                CagiranVardiya = CagiranVardiya,
                Sikayet = Sikayet.Trim(),
                Notlar = Notlar?.Trim() ?? ""
            };
            VeriDepolamaServisi.Instance.Acil112Kaydet(kayit);
            Msg("112 acil yardım çağrısı kaydedildi.", false);
            Temizle();
            YenileListe();
        }

        private void Guncelle()
        {
            if (_secili == null) return;
            var hata = FormDogrulama.Birlestir(
                FormDogrulama.MahkumKoduKontrol(MahkumKodu),
                FormDogrulama.ComboZorunlu(Vardiya, "Olayın vardiyası"),
                FormDogrulama.ComboZorunlu(CagiranVardiya, "112'yi arayan vardiya"),
                SikayetKontrol(Sikayet),
                FormDogrulama.AciklamaUzunluk(Notlar));
            if (!string.IsNullOrEmpty(hata)) { Msg(hata, true); return; }
            if (!CagriZamaniOlustur(out var cagriZamani, out var zHata))
            {
                Msg(zHata ?? "Zaman geçersiz.", true);
                return;
            }

            var m = VeriDepolamaServisi.Instance.MahkumBul(MahkumKodu);
            if (m == null) { Msg("Mahkum bulunamadı.", true); return; }

            _secili.MahkumId = m.Id;
            _secili.MahkumAdSoyad = m.AdSoyad;
            _secili.CagriZamani = cagriZamani;
            _secili.Vardiya = Vardiya;
            _secili.CagiranVardiya = CagiranVardiya;
            _secili.Sikayet = Sikayet.Trim();
            _secili.Notlar = Notlar?.Trim() ?? "";
            VeriDepolamaServisi.Instance.Acil112Kaydet(_secili);
            Msg("Kayıt güncellendi.", false);
            Temizle();
            YenileListe();
        }

        private void Sil()
        {
            if (_secili == null) return;
            VeriDepolamaServisi.Instance.Acil112SoftDelete(_secili.KayitId);
            Msg("Kayıt pasife alındı.", false);
            Temizle();
            YenileListe();
        }

        private void KayitSec(object? p)
        {
            if (p is not Acil112CagriKaydi k) return;
            _secili = k;
            DuzenleModu = true;
            MahkumKodu = k.MahkumKodu;
            CagriTarih = k.CagriZamani.Date;
            CagriSaat = k.CagriZamani.ToString("HH:mm", CultureInfo.InvariantCulture);
            VardiyaListesineEksikEkle(k.Vardiya);
            VardiyaListesineEksikEkle(k.CagiranVardiya);
            Vardiya = string.IsNullOrWhiteSpace(k.Vardiya) ? NobetVardiyalari.Tumunu[0] : k.Vardiya;
            CagiranVardiya = k.CagiranVardiya ?? "";
            Sikayet = k.Sikayet;
            Notlar = k.Notlar;
        }

        private void Temizle()
        {
            _secili = null;
            DuzenleModu = false;
            CagriTarih = DateTime.Today;
            CagriSaat = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
            Vardiya = NobetVardiyalari.Tumunu[0];
            CagiranVardiya = NobetVardiyalari.Tumunu[0];
            Sikayet = "";
            Notlar = "";
        }

        private void VardiyaListesineEksikEkle(string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger) || VardiyaListesi.Contains(deger)) return;
            VardiyaListesi.Add(deger);
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
