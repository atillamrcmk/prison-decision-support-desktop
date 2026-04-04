using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using KKDS.Models;
using KKDS.Services;

namespace KKDS.Data
{
    public sealed class JsonDataRepository : IDataRepository
    {
        private static readonly Lazy<JsonDataRepository> _instance = new(() => new JsonDataRepository());
        public static JsonDataRepository Instance => _instance.Value;

        private readonly JsonSerializerOptions _jsonOpt;
        private readonly LogServisi _log = LogServisi.Instance;

        private List<Mahkum> _mahkumlar = new();
        private List<Olay> _olaylar = new();
        private List<PsikologDegerlendirme> _psikologlar = new();
        private List<RevirKaydi> _revirler = new();
        private List<KurulKarari> _kararlar = new();
        private List<Kullanici> _kullanicilar = new();

        public string VeriKokYolu { get; private set; } = "";

        public DateTime SonVeriYuklemeZamani { get; private set; }
        public int SonYuklemeOkunanDosyaSayisi { get; private set; }
        public int SonYuklemeHataliDosyaSayisi { get; private set; }

        public IReadOnlyList<Mahkum> Mahkumlar =>
            _mahkumlar.Where(m => m.AktifMi && !m.IsDemoData).ToList().AsReadOnly();

        public IReadOnlyList<Mahkum> TumMahkumlar => _mahkumlar.AsReadOnly();

        public IReadOnlyList<Olay> Olaylar =>
            _olaylar.Where(o => o.AktifMi && !o.IsDemoData).ToList().AsReadOnly();

        public IReadOnlyList<Olay> OlaylarDahilDemo =>
            _olaylar.Where(o => o.AktifMi).ToList().AsReadOnly();

        public IReadOnlyList<PsikologDegerlendirme> PsikologDegerlendirmeleri =>
            _psikologlar.Where(p => p.AktifMi && !p.IsDemoData).ToList().AsReadOnly();

        public IReadOnlyList<RevirKaydi> RevirKayitlari =>
            _revirler.Where(r => r.AktifMi && !r.IsDemoData).ToList().AsReadOnly();

        public IReadOnlyList<KurulKarari> KurulKararlari =>
            _kararlar.Where(k => k.AktifMi && !k.IsDemoData).ToList().AsReadOnly();

        public IReadOnlyList<KurulKarari> KurulKararlariDahilDemo =>
            _kararlar.Where(k => k.AktifMi).ToList().AsReadOnly();

        public IReadOnlyList<Kullanici> KullanicilarListesi => _kullanicilar.AsReadOnly();

        private JsonDataRepository()
        {
            _jsonOpt = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            VeriKokYolu = UygulamaAyarlariServisi.Instance.VeriKlasoruYolu;
            EnsureDirectories();
            LoadKullanicilar();
        }

        public void VeriYolunuYenile()
        {
            VeriKokYolu = UygulamaAyarlariServisi.Instance.VeriKlasoruYolu;
            EnsureDirectories();
            LoadKullanicilar();
            TumVerileriYukle();
        }

        private void EnsureDirectories()
        {
            string[] dirs = { "psikolog", "revir", "disiplin", "kurul", "mahkumlar", "islenmis", "logs", "kullanicilar", "raporlar", "yedek" };
            foreach (var d in dirs)
                Directory.CreateDirectory(Path.Combine(VeriKokYolu, d));
        }

        #region Kullanici

        private void LoadKullanicilar()
        {
            var path = Path.Combine(VeriKokYolu, "kullanicilar");
            if (!Directory.GetFiles(path, "*.json").Any())
            {
                var defaults = new[]
                {
                    ("psikolog1", "1234", Roller.Psikolog, "Dr. Ayşe Yılmaz"),
                    ("revir1", "1234", Roller.Revir, "Hemşire Fatma Kaya"),
                    ("disiplin1", "1234", Roller.Disiplin, "Gardiyan Mehmet Demir"),
                    ("yonetici1", "1234", Roller.Yonetici, "Müdür Ali Öztürk"),
                    ("admin", "admin", Roller.Yonetici, "Sistem Yöneticisi")
                };
                foreach (var (adi, sifre, rol, adSoyad) in defaults)
                {
                    var tuz = SifreServisi.TuzOlustur();
                    var k = new Kullanici
                    {
                        KullaniciAdi = adi,
                        Sifre = "",
                        SifreTuzu = tuz,
                        SifreHash = SifreServisi.HashOlustur(sifre, tuz),
                        Rol = rol,
                        AdSoyad = adSoyad
                    };
                    File.WriteAllText(Path.Combine(path, $"{k.KullaniciAdi}.json"), JsonSerializer.Serialize(k, _jsonOpt));
                }
            }
            _kullanicilar.Clear();
            foreach (var f in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var k = JsonSerializer.Deserialize<Kullanici>(File.ReadAllText(f), _jsonOpt);
                    if (k != null) _kullanicilar.Add(k);
                }
                catch { }
            }
        }

        public Kullanici? GirisYap(string adi, string sifre)
        {
            var k = _kullanicilar.FirstOrDefault(x => x.KullaniciAdi.Equals(adi, StringComparison.OrdinalIgnoreCase));
            if (k == null) return null;

            if (k.HashKullaniliyor)
            {
                if (SifreServisi.Dogrula(sifre, k.SifreHash, k.SifreTuzu)) return k;
                return null;
            }

            if (!string.IsNullOrEmpty(k.Sifre) && k.Sifre == sifre)
            {
                var tuz = SifreServisi.TuzOlustur();
                k.SifreTuzu = tuz;
                k.SifreHash = SifreServisi.HashOlustur(sifre, tuz);
                k.Sifre = "";
                KullaniciKaydet(k);
                return k;
            }

            return null;
        }

        public bool KullaniciVarMi(string adi) =>
            _kullanicilar.Any(k => k.KullaniciAdi.Equals(adi, StringComparison.OrdinalIgnoreCase));

        public void KullaniciKaydet(Kullanici k)
        {
            if (!string.IsNullOrEmpty(k.Sifre))
            {
                var tuz = SifreServisi.TuzOlustur();
                k.SifreTuzu = tuz;
                k.SifreHash = SifreServisi.HashOlustur(k.Sifre, tuz);
                k.Sifre = "";
            }

            var existing = _kullanicilar.FindIndex(x => x.KullaniciAdi.Equals(k.KullaniciAdi, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
                _kullanicilar[existing] = k;
            else
                _kullanicilar.Add(k);

            var path = Path.Combine(VeriKokYolu, "kullanicilar", $"{k.KullaniciAdi}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(k, _jsonOpt));
            _log.KritikIslem(LogIslemTipleri.KullaniciKayit, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "kullanici", null, $"Kullanıcı kaydedildi: {k.KullaniciAdi} ({Roller.RolAdi(k.Rol)})");
        }

        public void KullaniciSil(string kullaniciAdi)
        {
            var k = _kullanicilar.FirstOrDefault(x => x.KullaniciAdi.Equals(kullaniciAdi, StringComparison.OrdinalIgnoreCase));
            if (k == null) return;
            _kullanicilar.Remove(k);

            var path = Path.Combine(VeriKokYolu, "kullanicilar", $"{k.KullaniciAdi}.json");
            if (File.Exists(path)) File.Delete(path);
            _log.KritikIslem(LogIslemTipleri.KullaniciSil, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "kullanici", null, $"Kullanıcı silindi: {kullaniciAdi}");
        }

        public void KullanicilariYenidenYukle() => LoadKullanicilar();

        #endregion

        #region Toplu Yükleme

        public void TumVerileriYukle()
        {
            int ok = 0, hata = 0;
            _mahkumlar = LoadAll<Mahkum>("mahkumlar", ref ok, ref hata);
            _olaylar = LoadAll<Olay>("disiplin", ref ok, ref hata);
            _psikologlar = LoadAll<PsikologDegerlendirme>("psikolog", ref ok, ref hata);
            _revirler = LoadAll<RevirKaydi>("revir", ref ok, ref hata);
            _kararlar = LoadAll<KurulKarari>("kurul", ref ok, ref hata);
            SonYuklemeOkunanDosyaSayisi = ok;
            SonYuklemeHataliDosyaSayisi = hata;
            SonVeriYuklemeZamani = DateTime.Now;
        }

        private List<T> LoadAll<T>(string folder, ref int ok, ref int hata)
        {
            var list = new List<T>();
            var path = Path.Combine(VeriKokYolu, folder);
            if (!Directory.Exists(path)) return list;
            foreach (var f in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var item = JsonSerializer.Deserialize<T>(File.ReadAllText(f), _jsonOpt);
                    if (item != null) { list.Add(item); ok++; }
                }
                catch { hata++; }
            }
            return list;
        }

        public bool DemoMahkumVarMi() => _mahkumlar.Any(m => m.IsDemoData);

        public void DemoVerisiniTemizle()
        {
            void SilDemo(string folder, Func<string, bool> isDemo)
            {
                var path = Path.Combine(VeriKokYolu, folder);
                if (!Directory.Exists(path)) return;
                foreach (var f in Directory.GetFiles(path, "*.json"))
                {
                    try
                    {
                        if (isDemo(File.ReadAllText(f))) File.Delete(f);
                    }
                    catch { }
                }
            }

            SilDemo("mahkumlar", j => JsonSerializer.Deserialize<Mahkum>(j, _jsonOpt)?.IsDemoData == true);
            SilDemo("disiplin", j => JsonSerializer.Deserialize<Olay>(j, _jsonOpt)?.IsDemoData == true);
            SilDemo("psikolog", j => JsonSerializer.Deserialize<PsikologDegerlendirme>(j, _jsonOpt)?.IsDemoData == true);
            SilDemo("revir", j => JsonSerializer.Deserialize<RevirKaydi>(j, _jsonOpt)?.IsDemoData == true);
            SilDemo("kurul", j => JsonSerializer.Deserialize<KurulKarari>(j, _jsonOpt)?.IsDemoData == true);

            TumVerileriYukle();
            _log.KritikIslem(LogIslemTipleri.DemoTemizle, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "sistem", null, "Demo verisi temizlendi");
        }

        public void DemoVerisiYukle()
        {
            TestVeriOlusturucu.Olustur();
            TumVerileriYukle();
            _log.KritikIslem(LogIslemTipleri.DemoYukle, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "sistem", null, "Demo verisi yüklendi");
        }

        #endregion

        #region Mahkum CRUD

        public void MahkumKaydet(Mahkum m)
        {
            if (m.Id == 0) m.Id = _mahkumlar.Any() ? _mahkumlar.Max(x => x.Id) + 1 : 1;
            if (string.IsNullOrEmpty(m.MahkumKodu)) m.MahkumKodu = $"M-{DateTime.Now:yyyy}{m.Id:D3}";
            m.KayitTuru = "mahkum";

            var existing = _mahkumlar.FindIndex(x => x.Id == m.Id);
            if (existing >= 0)
            {
                OturumBilgisi.Instance.MetaGuncelle(m);
                _mahkumlar[existing] = m;
                _log.KritikIslem(LogIslemTipleri.KayitGuncelle, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "mahkum", m.MahkumKodu, "Mahkum güncellendi");
            }
            else
            {
                OturumBilgisi.Instance.MetaDoldurYeni(m);
                _mahkumlar.Add(m);
                _log.KritikIslem(LogIslemTipleri.KayitOlustur, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "mahkum", m.MahkumKodu, "Mahkum oluşturuldu");
            }

            SaveJson("mahkumlar", $"mahkum_{m.Id}.json", m);
        }

        public void MahkumSoftDelete(int id)
        {
            var m = _mahkumlar.FirstOrDefault(x => x.Id == id);
            if (m == null) return;
            OturumBilgisi.Instance.MetaGuncelle(m);
            m.AktifMi = false;
            SaveJson("mahkumlar", $"mahkum_{m.Id}.json", m);
            _log.KritikIslem(LogIslemTipleri.KayitPasif, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "mahkum", m.MahkumKodu, "Mahkum pasife alındı");
        }

        public Mahkum? MahkumBul(string kod) =>
            _mahkumlar.FirstOrDefault(m => m.AktifMi && m.MahkumKodu.Equals(kod, StringComparison.OrdinalIgnoreCase));

        public Mahkum? MahkumBulById(int id) =>
            _mahkumlar.FirstOrDefault(m => m.Id == id);

        public List<Mahkum> MahkumAra(string arama) =>
            MahkumlariGetir(demoDahil: true, pasifDahil: false)
                .Where(m => m.MahkumKodu.Contains(arama, StringComparison.OrdinalIgnoreCase) ||
                            m.AdSoyad.Contains(arama, StringComparison.OrdinalIgnoreCase))
                .ToList();

        public List<Mahkum> MahkumlariGetir(bool demoDahil, bool pasifDahil)
        {
            IEnumerable<Mahkum> q = _mahkumlar;
            if (!pasifDahil) q = q.Where(m => m.AktifMi);
            if (!demoDahil) q = q.Where(m => !m.IsDemoData);
            return q.OrderBy(m => m.MahkumKodu).ToList();
        }

        #endregion

        #region Olay CRUD

        public void OlayKaydet(Olay o)
        {
            if (o.Id == 0) o.Id = _olaylar.Any() ? _olaylar.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(o.MahkumId);
            if (mahkum != null) o.MahkumKodu = mahkum.MahkumKodu;

            var existing = _olaylar.FindIndex(x => x.KayitId == o.KayitId);
            if (existing >= 0)
            {
                OturumBilgisi.Instance.MetaGuncelle(o);
                _olaylar[existing] = o;
                _log.KritikIslem(LogIslemTipleri.KayitGuncelle, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "disiplin", o.MahkumKodu, $"Olay güncellendi: {o.OlayTuru}");
            }
            else
            {
                OturumBilgisi.Instance.MetaDoldurYeni(o);
                if (o.IsDemoData)
                    DemoKayitMetaAta(o, Roller.Disiplin, "disiplin1");
                _olaylar.Add(o);
                _log.KritikIslem(LogIslemTipleri.KayitOlustur, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "disiplin", o.MahkumKodu, $"Olay oluşturuldu: {o.OlayTuru}");
            }

            SaveJson("disiplin", $"olay_{o.KayitId}.json", o);
        }

        public void OlaySoftDelete(string kayitId)
        {
            var o = _olaylar.FirstOrDefault(x => x.KayitId == kayitId);
            if (o == null) return;
            OturumBilgisi.Instance.MetaGuncelle(o);
            o.AktifMi = false;
            SaveJson("disiplin", $"olay_{o.KayitId}.json", o);
            _log.KritikIslem(LogIslemTipleri.KayitPasif, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "disiplin", o.MahkumKodu, "Olay pasife alındı");
        }

        #endregion

        #region Psikolog CRUD

        public void PsikologKaydet(PsikologDegerlendirme p)
        {
            if (p.Id == 0) p.Id = _psikologlar.Any() ? _psikologlar.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(p.MahkumId);
            if (mahkum != null) p.MahkumKodu = mahkum.MahkumKodu;

            var existing = _psikologlar.FindIndex(x => x.KayitId == p.KayitId);
            if (existing >= 0)
            {
                OturumBilgisi.Instance.MetaGuncelle(p);
                _psikologlar[existing] = p;
                _log.KritikIslem(LogIslemTipleri.KayitGuncelle, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "psikolog", p.MahkumKodu, "Psikolog kaydı güncellendi");
            }
            else
            {
                OturumBilgisi.Instance.MetaDoldurYeni(p);
                if (p.IsDemoData)
                    DemoKayitMetaAta(p, Roller.Psikolog, "psikolog1");
                _psikologlar.Add(p);
                _log.KritikIslem(LogIslemTipleri.KayitOlustur, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "psikolog", p.MahkumKodu, "Psikolog kaydı oluşturuldu");
            }

            SaveJson("psikolog", $"psikolog_{p.KayitId}.json", p);
        }

        public void PsikologSoftDelete(string kayitId)
        {
            var p = _psikologlar.FirstOrDefault(x => x.KayitId == kayitId);
            if (p == null) return;
            OturumBilgisi.Instance.MetaGuncelle(p);
            p.AktifMi = false;
            SaveJson("psikolog", $"psikolog_{p.KayitId}.json", p);
            _log.KritikIslem(LogIslemTipleri.KayitPasif, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "psikolog", p.MahkumKodu, "Psikolog kaydı pasife alındı");
        }

        #endregion

        #region Revir CRUD

        public void RevirKaydet(RevirKaydi r)
        {
            if (r.Id == 0) r.Id = _revirler.Any() ? _revirler.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(r.MahkumId);
            if (mahkum != null) r.MahkumKodu = mahkum.MahkumKodu;

            var existing = _revirler.FindIndex(x => x.KayitId == r.KayitId);
            if (existing >= 0)
            {
                OturumBilgisi.Instance.MetaGuncelle(r);
                _revirler[existing] = r;
                _log.KritikIslem(LogIslemTipleri.KayitGuncelle, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "revir", r.MahkumKodu, "Revir kaydı güncellendi");
            }
            else
            {
                OturumBilgisi.Instance.MetaDoldurYeni(r);
                if (r.IsDemoData)
                    DemoKayitMetaAta(r, Roller.Revir, "revir1");
                _revirler.Add(r);
                _log.KritikIslem(LogIslemTipleri.KayitOlustur, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "revir", r.MahkumKodu, "Revir kaydı oluşturuldu");
            }

            SaveJson("revir", $"revir_{r.KayitId}.json", r);
        }

        public void RevirSoftDelete(string kayitId)
        {
            var r = _revirler.FirstOrDefault(x => x.KayitId == kayitId);
            if (r == null) return;
            OturumBilgisi.Instance.MetaGuncelle(r);
            r.AktifMi = false;
            SaveJson("revir", $"revir_{r.KayitId}.json", r);
            _log.KritikIslem(LogIslemTipleri.KayitPasif, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                "revir", r.MahkumKodu, "Revir kaydı pasife alındı");
        }

        #endregion

        #region Kurul CRUD

        public void KurulKarariKaydet(KurulKarari k)
        {
            if (k.Id == 0) k.Id = _kararlar.Any() ? _kararlar.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(k.MahkumId);
            if (mahkum != null) k.MahkumKodu = mahkum.MahkumKodu;

            var existing = _kararlar.FindIndex(x => x.KayitId == k.KayitId);
            if (existing >= 0)
            {
                OturumBilgisi.Instance.MetaGuncelle(k);
                _kararlar[existing] = k;
                _log.KritikIslem(LogIslemTipleri.KayitGuncelle, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "kurul", k.MahkumKodu, $"Kurul kararı güncellendi: {k.KararTuru}");
            }
            else
            {
                OturumBilgisi.Instance.MetaDoldurYeni(k);
                _kararlar.Add(k);
                _log.KritikIslem(LogIslemTipleri.KayitOlustur, OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                    "kurul", k.MahkumKodu, $"Kurul kararı oluşturuldu: {k.KararTuru}");
            }

            SaveJson("kurul", $"karar_{k.KayitId}.json", k);
        }

        public void KurulOnayVer(string kararKayitId, string rol, string kullanici, string kullaniciAd, string durum, string yorum)
        {
            var karar = _kararlar.FirstOrDefault(k => k.KayitId == kararKayitId && k.AktifMi);
            if (karar == null) return;

            var onay = karar.Onaylar.FirstOrDefault(o => o.Rol == rol);
            if (onay == null) return;

            onay.Kullanici = kullanici;
            onay.KullaniciAd = kullaniciAd;
            onay.Durum = durum;
            onay.Yorum = yorum;
            onay.Tarih = DateTime.Now;

            karar.OnayDurumuGuncelle();
            OturumBilgisi.Instance.MetaGuncelle(karar);
            SaveJson("kurul", $"karar_{karar.KayitId}.json", karar);

            var durumMetin = durum == OnayDurumlari.Onaylandi ? "onayladı" : "reddetti";
            _log.KritikIslem(LogIslemTipleri.KurulOnay, kullanici, rol,
                "kurul", karar.MahkumKodu, $"Kurul kararı {durumMetin}: {karar.KararTuru}");
        }

        public List<KurulKarari> BekleyenKararlar() =>
            _kararlar.Where(k => k.AktifMi && k.OnayDurumu == OnayDurumlari.Beklemede)
                     .OrderByDescending(k => k.KararTarihi).ToList();

        public List<KurulKarari> RolIcinBekleyenKararlar(string rol) =>
            _kararlar.Where(k => k.AktifMi &&
                k.OnayDurumu == OnayDurumlari.Beklemede &&
                k.Onaylar.Any(o => o.Rol == rol && o.Durum == OnayDurumlari.Beklemede))
                .OrderByDescending(k => k.KararTarihi).ToList();

        public List<KurulKarari> OnaylanmisKararlar() =>
            _kararlar.Where(k => k.AktifMi && k.OnayDurumu == OnayDurumlari.Onaylandi)
                     .OrderByDescending(k => k.KesinlesmeTarihi).ToList();

        public List<KurulKarari> TumKurulKararlari() =>
            _kararlar.Where(k => k.AktifMi).OrderByDescending(k => k.KararTarihi).ToList();

        #endregion

        #region Sorgular

        public List<Olay> MahkumOlaylari(int mid) =>
            _olaylar.Where(o => o.AktifMi && o.MahkumId == mid).OrderByDescending(o => o.OlayTarihi).ToList();

        public List<PsikologDegerlendirme> MahkumPsikolog(int mid) =>
            _psikologlar.Where(p => p.AktifMi && p.MahkumId == mid).OrderByDescending(p => p.DegerlendirmeTarihi).ToList();

        public List<RevirKaydi> MahkumRevir(int mid) =>
            _revirler.Where(r => r.AktifMi && r.MahkumId == mid).OrderByDescending(r => r.KayitTarihi).ToList();

        public List<KurulKarari> MahkumKararlari(int mid) =>
            _kararlar.Where(k => k.AktifMi && k.MahkumId == mid).OrderByDescending(k => k.KararTarihi).ToList();

        public List<Olay> KullaniciOlaylari(string kullanici) =>
            _olaylar.Where(o => o.AktifMi &&
                (o.GirenKullanici == kullanici || (o.IsDemoData && kullanici == "disiplin1")))
                .OrderByDescending(o => o.OlayTarihi).ToList();

        public List<PsikologDegerlendirme> KullaniciPsikologKayitlari(string kullanici) =>
            _psikologlar.Where(p => p.AktifMi &&
                (p.GirenKullanici == kullanici || (p.IsDemoData && kullanici == "psikolog1")))
                .OrderByDescending(p => p.DegerlendirmeTarihi).ToList();

        public List<RevirKaydi> KullaniciRevirKayitlari(string kullanici) =>
            _revirler.Where(r => r.AktifMi &&
                (r.GirenKullanici == kullanici || (r.IsDemoData && kullanici == "revir1")))
                .OrderByDescending(r => r.KayitTarihi).ToList();

        public List<BaseKayit> TumKayitlar(bool demoDahil)
        {
            IEnumerable<BaseKayit> hepsi = Enumerable.Empty<BaseKayit>();
            IEnumerable<Olay> ol = _olaylar.Where(o => o.AktifMi);
            IEnumerable<PsikologDegerlendirme> ps = _psikologlar.Where(p => p.AktifMi);
            IEnumerable<RevirKaydi> rv = _revirler.Where(r => r.AktifMi);
            IEnumerable<KurulKarari> ku = _kararlar.Where(k => k.AktifMi);
            if (!demoDahil)
            {
                ol = ol.Where(o => !o.IsDemoData);
                ps = ps.Where(p => !p.IsDemoData);
                rv = rv.Where(r => !r.IsDemoData);
                ku = ku.Where(k => !k.IsDemoData);
            }
            var list = new List<BaseKayit>();
            list.AddRange(ol);
            list.AddRange(ps);
            list.AddRange(rv);
            list.AddRange(ku);
            return list.OrderByDescending(k => k.OlusturmaZamani).ToList();
        }

        #endregion

        /// <summary>Demo yüklenirken oturum yönetici olabilir; geçmiş ekranlarında rol kullanıcıları kayıtları görebilsin diye demo kayıtları sabit demo hesaplarına bağlanır.</summary>
        private static void DemoKayitMetaAta(BaseKayit k, string girenRol, string kullaniciAdi)
        {
            k.GirenKullanici = kullaniciAdi;
            k.GirenRol = girenRol;
            k.OlusturanKullanici = kullaniciAdi;
        }

        private void SaveJson<T>(string folder, string fileName, T data)
        {
            var path = Path.Combine(VeriKokYolu, folder, fileName);
            File.WriteAllText(path, JsonSerializer.Serialize(data, _jsonOpt));
        }
    }
}
