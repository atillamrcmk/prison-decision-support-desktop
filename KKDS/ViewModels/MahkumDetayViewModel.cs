using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KKDS.Helpers;
using KKDS.Models;
using KKDS.Services;
using Microsoft.Win32;

namespace KKDS.ViewModels
{
    public class MahkumDetayViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;
        private int _seciliSekme;

        public Mahkum Mahkum { get; set; } = new();
        public AnalizSonuc Analiz { get; set; } = new();
        public TahminSonuc Tahmin { get; set; } = new();
        public RiskSkoru Risk { get; set; } = new();

        public int SeciliSekme
        {
            get => _seciliSekme;
            set => SetProperty(ref _seciliSekme, value);
        }

        public ObservableCollection<HaftalikTrend> HaftalikTrendler { get; } = new();
        public ObservableCollection<TrendPeriyotOgesi> TrendPeriyotSecenekleri { get; } = new()
        {
            new TrendPeriyotOgesi { Deger = OlayTrendPeriyot.Gunluk, Ad = "Günlük (8 gün)" },
            new TrendPeriyotOgesi { Deger = OlayTrendPeriyot.Haftalik, Ad = "Haftalık (8 hafta)" },
            new TrendPeriyotOgesi { Deger = OlayTrendPeriyot.Aylik, Ad = "Aylık (8 ay)" }
        };

        private OlayTrendPeriyot _trendPeriyot = OlayTrendPeriyot.Haftalik;
        private List<Olay> _mahkumOlaylariCache = new();

        public OlayTrendPeriyot TrendPeriyot
        {
            get => _trendPeriyot;
            set
            {
                if (SetProperty(ref _trendPeriyot, value))
                {
                    OnPropertyChanged(nameof(TrendGrafikBaslik));
                    OnPropertyChanged(nameof(TrendGrafikAciklama));
                    GuncelleTrendSerisi();
                }
            }
        }

        public string TrendGrafikBaslik => TrendPeriyot switch
        {
            OlayTrendPeriyot.Gunluk => "Günlük olay trendi",
            OlayTrendPeriyot.Aylik => "Aylık olay trendi",
            _ => "Haftalık olay trendi"
        };

        public string TrendGrafikAciklama => TrendPeriyot switch
        {
            OlayTrendPeriyot.Gunluk => "Her sütun bir günün olay sayısı; eksende ilgili günün tarihi (gg.aa.yyyy).",
            OlayTrendPeriyot.Aylik => "Her sütun bir ayın olay sayısı; eksende ay adı ve yıl.",
            _ => "Her sütun 7 günlük dilimin olay sayısı; eksende haftanın başlangıç–bitiş tarihleri."
        };
        public ObservableCollection<OlayTurDagilim> OlayTurDagilimi { get; } = new();
        public ObservableCollection<Olay> Olaylar { get; } = new();
        public ObservableCollection<PsikologDegerlendirme> PsikologKayitlari { get; } = new();
        public ObservableCollection<RevirKaydi> RevirKayitlari { get; } = new();
        public ObservableCollection<KurulKarari> KurulKararlari { get; } = new();
        public ObservableCollection<Acil112CagriKaydi> Acil112Kayitlari { get; } = new();
        public ObservableCollection<ZamanAkisiOgesi> ZamanAkisi { get; } = new();

        public int ToplamOlay { get; set; }
        public int KararOncesiOlay { get; set; }
        public int KararSonrasiOlay { get; set; }
        public string PsikologDurum { get; set; } = "-";
        public string RevirDurum { get; set; } = "-";
        public string ZamanAkisiSonuc { get; set; } = "";

        public ICommand GeriCommand { get; }
        public ICommand SekmeCommand { get; }
        public ICommand DisaAktarPdfCommand { get; }

        public MahkumDetayViewModel(int mahkumId, MainViewModel main)
        {
            _main = main;
            GeriCommand = new RelayCommand(() => main.MahkumListeGoster());
            SekmeCommand = new RelayCommand(p =>
            {
                if (p is string s && int.TryParse(s, out int idx))
                    SeciliSekme = idx;
            });
            DisaAktarPdfCommand = new RelayCommand(_ => DisaAktarPdf());
            YetkiServisi.ViewModelKoruma(this, Roller.Yonetici);
            if (!YetkisizMod) Yukle(mahkumId);
        }

        private void DisaAktarPdf()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                FileName = $"Mahkum_{Mahkum.MahkumKodu}_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var veri = new MahkumDetayPdfVeri
                {
                    Mahkum = Mahkum,
                    Analiz = Analiz,
                    Tahmin = Tahmin,
                    Risk = Risk,
                    HaftalikTrend = Analiz.HaftalikTrendler.ToList(),
                    OlayTurDagilimi = OlayTurDagilimi.Select(x => new OlayTurDagilimSatir { Tur = x.Tur, Sayi = x.Sayi }).ToList(),
                    SistemOzeti = Analiz.SistemOzeti ?? new List<string>(),
                    Olaylar = Olaylar.OrderByDescending(o => o.OlayTarihi).ToList()
                };
                MahkumDetayPdfServisi.Olustur(dlg.FileName, veri);
                LogServisi.Instance.KritikIslem(LogIslemTipleri.RaporPdf, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "pdf", Mahkum.MahkumKodu, $"Mahkum detay PDF: {dlg.FileName}");
                MessageBox.Show($"PDF kaydedildi:\n{dlg.FileName}", "Dışa aktar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF oluşturulamadı:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Yukle(int mahkumId)
        {
            var veri = VeriDepolamaServisi.Instance;
            var analizServisi = AnalizServisi.Instance;
            var tahminServisi = TahminServisi.Instance;
            var riskServisi = RiskServisi.Instance;

            Mahkum = veri.MahkumBulById(mahkumId) ?? new Mahkum();
            Analiz = analizServisi.TamAnaliz(mahkumId);
            Tahmin = tahminServisi.YediGunlukTahmin(mahkumId);
            Risk = riskServisi.RiskSkoruHesapla(mahkumId);

            var olaylar = veri.MahkumOlaylari(mahkumId);
            _mahkumOlaylariCache = olaylar;
            GuncelleTrendSerisi();

            // Olay türü dağılımı
            var gruplar = olaylar.GroupBy(o => o.OlayTuru).OrderByDescending(g => g.Count());
            foreach (var g in gruplar)
                OlayTurDagilimi.Add(new OlayTurDagilim { Tur = g.Key, Sayi = g.Count() });

            // Sekmeli ham veriler
            foreach (var o in olaylar) Olaylar.Add(o);
            foreach (var p in veri.MahkumPsikolog(mahkumId)) PsikologKayitlari.Add(p);
            foreach (var r in veri.MahkumRevir(mahkumId)) RevirKayitlari.Add(r);
            foreach (var a in veri.MahkumAcil112Kayitlari(mahkumId)) Acil112Kayitlari.Add(a);
            foreach (var k in veri.MahkumKararlari(mahkumId)) KurulKararlari.Add(k);

            // İstatistikler
            ToplamOlay = olaylar.Count;
            KararOncesiOlay = Analiz.KararEtkisi.OncesiOlaySayisi;
            KararSonrasiOlay = Analiz.KararEtkisi.SonrasiOlaySayisi;

            var sonPsikolog = veri.MahkumPsikolog(mahkumId).FirstOrDefault();
            PsikologDurum = sonPsikolog != null
                ? $"{sonPsikolog.RuhHali} - {sonPsikolog.OncekiDurumaGore}"
                : "Kayıt yok";

            var sonRevir = veri.MahkumRevir(mahkumId).FirstOrDefault();
            RevirDurum = sonRevir != null
                ? $"Uyku: {sonRevir.UykuDurumu}, Stres: {sonRevir.StresSeviyesi}/5"
                : "Kayıt yok";

            ZamanAkisi.Clear();
            var birlesik = new List<(DateTime t, string kaynak, string baslik, string detay)>();
            foreach (var o in olaylar)
                birlesik.Add((o.OlayTarihi, "Disiplin", o.OlayTuru, $"Şiddet {o.Siddet} · {Kisalt(o.Aciklama, 80)}"));
            foreach (var p in veri.MahkumPsikolog(mahkumId))
                birlesik.Add((p.DegerlendirmeTarihi, "Psikolog", p.OncekiDurumaGore, $"{p.RuhHali} · agresyon {p.AgresyonDuzeyi}"));
            foreach (var r in veri.MahkumRevir(mahkumId))
                birlesik.Add((r.KayitTarihi, "Revir", $"Uyku {r.UykuDurumu}", $"Stres {r.StresSeviyesi}/5 · {r.DavranisEtkisi} · {Kisalt(r.Aciklama, 60)}"));
            foreach (var a in veri.MahkumAcil112Kayitlari(mahkumId))
            {
                var arayan = string.IsNullOrWhiteSpace(a.CagiranVardiya) ? "—" : a.CagiranVardiya;
                birlesik.Add((a.CagriZamani, "112 Acil", $"Arayan: {arayan} · Olay: {a.Vardiya}", $"{Kisalt(a.Sikayet, 50)} · {Kisalt(a.Notlar, 60)}"));
            }

            foreach (var x in birlesik.OrderByDescending(x => x.t).Take(15).OrderBy(x => x.t))
                ZamanAkisi.Add(new ZamanAkisiOgesi { Tarih = x.t, Kaynak = x.kaynak, Baslik = x.baslik, Detay = x.detay });

            if (Analiz.Egilim == "artiyor")
                ZamanAkisiSonuc = "Son dönemde olay yoğunluğu artmaktadır.";
            else if (birlesik.Count(x => x.kaynak == "Disiplin" && x.t >= DateTime.Today.AddDays(-14)) >= 4)
                ZamanAkisiSonuc = "Son iki haftada disiplin kayıtları üst üste gelmektedir; yakın izleme önerilir.";
            else
                ZamanAkisiSonuc = "Zaman akışı kayıtları gözlemlendi; üst bölümdeki eğilim ve risk özeti birlikte değerlendirilmelidir.";
        }

        private static string Kisalt(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return "—";
            s = s.Replace('\r', ' ').Replace('\n', ' ');
            return s.Length <= max ? s : s[..(max - 1)] + "…";
        }

        private void GuncelleTrendSerisi()
        {
            HaftalikTrendler.Clear();
            foreach (var t in AnalizServisi.Instance.OlayTrendSerisiHesapla(_mahkumOlaylariCache, TrendPeriyot))
                HaftalikTrendler.Add(t);
        }
    }

    public class OlayTurDagilim
    {
        public string Tur { get; set; } = "";
        public int Sayi { get; set; }
    }

    public class ZamanAkisiOgesi
    {
        public DateTime Tarih { get; set; }
        public string Kaynak { get; set; } = "";
        public string Baslik { get; set; } = "";
        public string Detay { get; set; } = "";
        public string TarihMetni => Tarih.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
    }
}
