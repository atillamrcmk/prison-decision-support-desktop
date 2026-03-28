using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.ViewModels
{
    public class OnayKararSatir
    {
        public string KayitId { get; set; } = "";
        public string MahkumKodu { get; set; } = "";
        public string MahkumAd { get; set; } = "";
        public string KararTuru { get; set; } = "";
        public DateTime KararTarihi { get; set; }
        public string KisaGerekce { get; set; } = "";
        public string DetayliGerekce { get; set; } = "";
        public int SureGun { get; set; }
        public string OlusturanKullanici { get; set; } = "";
        public string OnayDurumuMetin { get; set; } = "";
        public string OnayOzet { get; set; } = "";
        public string PsikologDurum { get; set; } = "";
        public string PsikologYorum { get; set; } = "";
        public string RevirDurum { get; set; } = "";
        public string RevirYorum { get; set; } = "";
        public string DisiplinDurum { get; set; } = "";
        public string DisiplinYorum { get; set; } = "";
        public bool BenOnayBekliyor { get; set; }
        public bool BenOnayVermis { get; set; }
    }

    public class KurulOnaylarViewModel : BaseViewModel
    {
        private OnayKararSatir? _seciliKarar;
        private string _onayYorum = "";
        private string _mesaj = "";
        private bool _mesajHata;
        private string _filtre = "Bekleyen";

        public OnayKararSatir? SeciliKarar { get => _seciliKarar; set { SetProperty(ref _seciliKarar, value); OnPropertyChanged(nameof(KararSecili)); } }
        public bool KararSecili => SeciliKarar != null;
        public string OnayYorum { get => _onayYorum; set => SetProperty(ref _onayYorum, value); }
        public string Mesaj { get => _mesaj; set => SetProperty(ref _mesaj, value); }
        public bool MesajHata { get => _mesajHata; set => SetProperty(ref _mesajHata, value); }
        public string Filtre { get => _filtre; set { SetProperty(ref _filtre, value); Yukle(); } }

        public ObservableCollection<string> FiltreListesi { get; } = new() { "Bekleyen", "Tümü", "Onaylanan", "Reddedilen" };
        public ObservableCollection<OnayKararSatir> Kararlar { get; } = new();

        public ICommand OnaylaCommand { get; }
        public ICommand ReddetCommand { get; }
        public ICommand KararSecCommand { get; }

        private readonly string _aktifRol;

        public KurulOnaylarViewModel()
        {
            _aktifRol = OturumBilgisi.Instance.Rol;
            OnaylaCommand = new RelayCommand(Onayla, () => SeciliKarar?.BenOnayBekliyor == true);
            ReddetCommand = new RelayCommand(Reddet, () => SeciliKarar?.BenOnayBekliyor == true);
            KararSecCommand = new RelayCommand(p => KararSec(p));
            Yukle();
        }

        private void Yukle()
        {
            Kararlar.Clear();
            SeciliKarar = null;
            var veri = VeriDepolamaServisi.Instance;
            var oturum = OturumBilgisi.Instance;

            var kararlar = Filtre switch
            {
                "Bekleyen" => veri.BekleyenKararlar(),
                "Onaylanan" => veri.OnaylanmisKararlar(),
                "Reddedilen" => veri.TumKurulKararlari().Where(k => k.OnayDurumu == OnayDurumlari.Reddedildi).ToList(),
                _ => veri.TumKurulKararlari()
            };

            foreach (var k in kararlar)
            {
                var m = veri.MahkumBulById(k.MahkumId);
                var ps = k.Onaylar.FirstOrDefault(o => o.Rol == Roller.Psikolog);
                var rv = k.Onaylar.FirstOrDefault(o => o.Rol == Roller.Revir);
                var ds = k.Onaylar.FirstOrDefault(o => o.Rol == Roller.Disiplin);

                var benOnayBekliyor = k.OnayDurumu == OnayDurumlari.Beklemede &&
                    k.Onaylar.Any(o => o.Rol == _aktifRol && o.Durum == OnayDurumlari.Beklemede);

                var benOnayVermis = k.Onaylar.Any(o => o.Rol == _aktifRol && o.Durum != OnayDurumlari.Beklemede);

                Kararlar.Add(new OnayKararSatir
                {
                    KayitId = k.KayitId,
                    MahkumKodu = k.MahkumKodu,
                    MahkumAd = m?.AdSoyad ?? "-",
                    KararTuru = k.KararTuru,
                    KararTarihi = k.KararTarihi,
                    KisaGerekce = k.KisaGerekce,
                    DetayliGerekce = k.DetayliGerekce,
                    SureGun = k.SureGun,
                    OlusturanKullanici = k.GirenKullanici,
                    OnayDurumuMetin = OnayDurumlari.Goster(k.OnayDurumu),
                    OnayOzet = $"{k.OnaylayanSayisi}/{k.ToplamOnayGerekli}",
                    PsikologDurum = OnayMetin(ps),
                    PsikologYorum = ps?.Yorum ?? "",
                    RevirDurum = OnayMetin(rv),
                    RevirYorum = rv?.Yorum ?? "",
                    DisiplinDurum = OnayMetin(ds),
                    DisiplinYorum = ds?.Yorum ?? "",
                    BenOnayBekliyor = benOnayBekliyor,
                    BenOnayVermis = benOnayVermis
                });
            }
        }

        private void KararSec(object? p)
        {
            if (p is OnayKararSatir s) SeciliKarar = s;
        }

        private void Onayla()
        {
            if (SeciliKarar == null) return;
            if (string.IsNullOrWhiteSpace(OnayYorum))
            { Msg("Onay için yorum girmeniz gerekiyor.", true); return; }

            var oturum = OturumBilgisi.Instance;
            VeriDepolamaServisi.Instance.KurulOnayVer(
                SeciliKarar.KayitId, _aktifRol,
                oturum.KullaniciAdi, oturum.AdSoyad,
                OnayDurumlari.Onaylandi, OnayYorum);

            Msg($"Karar onaylandı: {SeciliKarar.MahkumKodu}", false);
            OnayYorum = "";
            Yukle();
        }

        private void Reddet()
        {
            if (SeciliKarar == null) return;
            if (string.IsNullOrWhiteSpace(OnayYorum))
            { Msg("Red için gerekçe girmeniz zorunludur.", true); return; }

            var oturum = OturumBilgisi.Instance;
            VeriDepolamaServisi.Instance.KurulOnayVer(
                SeciliKarar.KayitId, _aktifRol,
                oturum.KullaniciAdi, oturum.AdSoyad,
                OnayDurumlari.Reddedildi, OnayYorum);

            Msg($"Karar reddedildi: {SeciliKarar.MahkumKodu}", false);
            OnayYorum = "";
            Yukle();
        }

        private static string OnayMetin(KurulOnay? o)
        {
            if (o == null) return "-";
            return o.Durum switch
            {
                OnayDurumlari.Onaylandi => $"✔ {o.KullaniciAd}",
                OnayDurumlari.Reddedildi => $"✖ {o.KullaniciAd}",
                _ => "⏳ Bekliyor"
            };
        }

        private void Msg(string m, bool h) { Mesaj = m; MesajHata = h; }
    }
}
