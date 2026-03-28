using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using KKDS.Models;

namespace KKDS.Services
{
    public class VeriDepolamaServisi
    {
        private static readonly Lazy<VeriDepolamaServisi> _instance = new(() => new VeriDepolamaServisi());
        public static VeriDepolamaServisi Instance => _instance.Value;

        private readonly string _basePath;
        private readonly JsonSerializerOptions _jsonOpt;
        private readonly LogServisi _log = LogServisi.Instance;

        private List<Mahkum> _mahkumlar = new();
        private List<Olay> _olaylar = new();
        private List<PsikologDegerlendirme> _psikologlar = new();
        private List<RevirKaydi> _revirler = new();
        private List<KurulKarari> _kararlar = new();
        private List<Kullanici> _kullanicilar = new();

        public IReadOnlyList<Mahkum> Mahkumlar => _mahkumlar.Where(m => m.AktifMi).ToList().AsReadOnly();
        public IReadOnlyList<Mahkum> TumMahkumlar => _mahkumlar.AsReadOnly();
        public IReadOnlyList<Olay> Olaylar => _olaylar.Where(o => o.AktifMi).ToList().AsReadOnly();
        public IReadOnlyList<PsikologDegerlendirme> PsikologDegerlendirmeleri => _psikologlar.Where(p => p.AktifMi).ToList().AsReadOnly();
        public IReadOnlyList<RevirKaydi> RevirKayitlari => _revirler.Where(r => r.AktifMi).ToList().AsReadOnly();
        public IReadOnlyList<KurulKarari> KurulKararlari => _kararlar.Where(k => k.AktifMi).ToList().AsReadOnly();
        public IReadOnlyList<Kullanici> KullanicilarListesi => _kullanicilar.AsReadOnly();

        private VeriDepolamaServisi()
        {
            _basePath = @"C:\Paylasim_KKDS";
            _jsonOpt = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            EnsureDirectories();
            LoadKullanicilar();
        }

        private void EnsureDirectories()
        {
            string[] dirs = { "psikolog", "revir", "disiplin", "kurul", "mahkumlar", "islenmis", "logs", "kullanicilar", "raporlar", "yedek" };
            foreach (var d in dirs)
                Directory.CreateDirectory(Path.Combine(_basePath, d));
        }

        #region Kullanici
        private void LoadKullanicilar()
        {
            var path = Path.Combine(_basePath, "kullanicilar");
            if (!Directory.GetFiles(path, "*.json").Any())
            {
                var defaults = new List<Kullanici>
                {
                    new() { KullaniciAdi = "psikolog1", Sifre = "1234", Rol = Roller.Psikolog, AdSoyad = "Dr. Ayşe Yılmaz" },
                    new() { KullaniciAdi = "revir1", Sifre = "1234", Rol = Roller.Revir, AdSoyad = "Hemşire Fatma Kaya" },
                    new() { KullaniciAdi = "disiplin1", Sifre = "1234", Rol = Roller.Disiplin, AdSoyad = "Gardiyan Mehmet Demir" },
                    new() { KullaniciAdi = "yonetici1", Sifre = "1234", Rol = Roller.Yonetici, AdSoyad = "Müdür Ali Öztürk" },
                    new() { KullaniciAdi = "admin", Sifre = "admin", Rol = Roller.Yonetici, AdSoyad = "Sistem Yöneticisi" }
                };
                foreach (var k in defaults)
                    File.WriteAllText(Path.Combine(path, $"{k.KullaniciAdi}.json"), JsonSerializer.Serialize(k, _jsonOpt));
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

        public Kullanici? GirisYap(string adi, string sifre) =>
            _kullanicilar.FirstOrDefault(k => k.KullaniciAdi.Equals(adi, StringComparison.OrdinalIgnoreCase) && k.Sifre == sifre);

        public bool KullaniciVarMi(string adi) =>
            _kullanicilar.Any(k => k.KullaniciAdi.Equals(adi, StringComparison.OrdinalIgnoreCase));

        public void KullaniciKaydet(Kullanici k)
        {
            var existing = _kullanicilar.FindIndex(x => x.KullaniciAdi.Equals(k.KullaniciAdi, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
                _kullanicilar[existing] = k;
            else
                _kullanicilar.Add(k);

            var path = Path.Combine(_basePath, "kullanicilar", $"{k.KullaniciAdi}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(k, _jsonOpt));
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                $"Kullanıcı kaydedildi: {k.KullaniciAdi} ({Roller.RolAdi(k.Rol)})");
        }

        public void KullaniciSil(string kullaniciAdi)
        {
            var k = _kullanicilar.FirstOrDefault(x => x.KullaniciAdi.Equals(kullaniciAdi, StringComparison.OrdinalIgnoreCase));
            if (k == null) return;
            _kullanicilar.Remove(k);

            var path = Path.Combine(_basePath, "kullanicilar", $"{k.KullaniciAdi}.json");
            if (File.Exists(path)) File.Delete(path);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol,
                $"Kullanıcı silindi: {kullaniciAdi}");
        }

        public void KullanicilariYenidenYukle() => LoadKullanicilar();
        #endregion

        #region Toplu Yükleme
        public void TumVerileriYukle()
        {
            _mahkumlar = LoadAll<Mahkum>("mahkumlar");
            _olaylar = LoadAll<Olay>("disiplin");
            _psikologlar = LoadAll<PsikologDegerlendirme>("psikolog");
            _revirler = LoadAll<RevirKaydi>("revir");
            _kararlar = LoadAll<KurulKarari>("kurul");
        }

        private List<T> LoadAll<T>(string folder)
        {
            var list = new List<T>();
            var path = Path.Combine(_basePath, folder);
            if (!Directory.Exists(path)) return list;
            foreach (var f in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var item = JsonSerializer.Deserialize<T>(File.ReadAllText(f), _jsonOpt);
                    if (item != null) list.Add(item);
                }
                catch { }
            }
            return list;
        }

        public bool TestVerisiVarMi() => _mahkumlar.Any();

        public void TestVerisiniTemizle()
        {
            string[] folders = { "mahkumlar", "disiplin", "psikolog", "revir", "kurul" };
            foreach (var folder in folders)
            {
                var path = Path.Combine(_basePath, folder);
                if (Directory.Exists(path))
                    foreach (var f in Directory.GetFiles(path, "*.json"))
                        try { File.Delete(f); } catch { }
            }
            _mahkumlar.Clear(); _olaylar.Clear(); _psikologlar.Clear(); _revirler.Clear(); _kararlar.Clear();
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, "Test verisi temizlendi");
        }
        #endregion

        #region Mahkum CRUD
        public void MahkumKaydet(Mahkum m)
        {
            if (m.Id == 0) m.Id = _mahkumlar.Any() ? _mahkumlar.Max(x => x.Id) + 1 : 1;
            if (string.IsNullOrEmpty(m.MahkumKodu)) m.MahkumKodu = $"M-{DateTime.Now:yyyy}{m.Id:D3}";
            m.KayitTuru = "mahkum";
            OturumBilgisi.Instance.MetaDoldur(m);

            var existing = _mahkumlar.FindIndex(x => x.Id == m.Id);
            if (existing >= 0) { m.GuncellemeZamani = DateTime.Now; _mahkumlar[existing] = m; }
            else _mahkumlar.Add(m);

            SaveJson("mahkumlar", $"mahkum_{m.Id}.json", m);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Mahkum kaydedildi: {m.MahkumKodu}");
        }

        public void MahkumSoftDelete(int id)
        {
            var m = _mahkumlar.FirstOrDefault(x => x.Id == id);
            if (m == null) return;
            m.AktifMi = false;
            m.GuncellemeZamani = DateTime.Now;
            SaveJson("mahkumlar", $"mahkum_{m.Id}.json", m);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Mahkum pasife alındı: {m.MahkumKodu}");
        }

        public Mahkum? MahkumBul(string kod) =>
            _mahkumlar.FirstOrDefault(m => m.AktifMi && m.MahkumKodu.Equals(kod, StringComparison.OrdinalIgnoreCase));

        public Mahkum? MahkumBulById(int id) =>
            _mahkumlar.FirstOrDefault(m => m.Id == id);

        public List<Mahkum> MahkumAra(string arama) =>
            _mahkumlar.Where(m => m.AktifMi &&
                (m.MahkumKodu.Contains(arama, StringComparison.OrdinalIgnoreCase) ||
                 m.AdSoyad.Contains(arama, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        #endregion

        #region Olay CRUD
        public void OlayKaydet(Olay o)
        {
            if (o.Id == 0) o.Id = _olaylar.Any() ? _olaylar.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(o.MahkumId);
            if (mahkum != null) o.MahkumKodu = mahkum.MahkumKodu;
            OturumBilgisi.Instance.MetaDoldur(o);

            var existing = _olaylar.FindIndex(x => x.KayitId == o.KayitId);
            if (existing >= 0) { o.GuncellemeZamani = DateTime.Now; _olaylar[existing] = o; }
            else _olaylar.Add(o);

            SaveJson("disiplin", $"olay_{o.KayitId}.json", o);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Olay kaydedildi: {o.MahkumKodu} - {o.OlayTuru}");
        }

        public void OlaySoftDelete(string kayitId)
        {
            var o = _olaylar.FirstOrDefault(x => x.KayitId == kayitId);
            if (o == null) return;
            o.AktifMi = false; o.GuncellemeZamani = DateTime.Now;
            SaveJson("disiplin", $"olay_{o.KayitId}.json", o);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Olay pasife alındı: {o.KayitId}");
        }
        #endregion

        #region Psikolog CRUD
        public void PsikologKaydet(PsikologDegerlendirme p)
        {
            if (p.Id == 0) p.Id = _psikologlar.Any() ? _psikologlar.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(p.MahkumId);
            if (mahkum != null) p.MahkumKodu = mahkum.MahkumKodu;
            OturumBilgisi.Instance.MetaDoldur(p);

            var existing = _psikologlar.FindIndex(x => x.KayitId == p.KayitId);
            if (existing >= 0) { p.GuncellemeZamani = DateTime.Now; _psikologlar[existing] = p; }
            else _psikologlar.Add(p);

            SaveJson("psikolog", $"psikolog_{p.KayitId}.json", p);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Psikolog kaydı: {p.MahkumKodu}");
        }

        public void PsikologSoftDelete(string kayitId)
        {
            var p = _psikologlar.FirstOrDefault(x => x.KayitId == kayitId);
            if (p == null) return;
            p.AktifMi = false; p.GuncellemeZamani = DateTime.Now;
            SaveJson("psikolog", $"psikolog_{p.KayitId}.json", p);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Psikolog kaydı pasife alındı: {p.KayitId}");
        }
        #endregion

        #region Revir CRUD
        public void RevirKaydet(RevirKaydi r)
        {
            if (r.Id == 0) r.Id = _revirler.Any() ? _revirler.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(r.MahkumId);
            if (mahkum != null) r.MahkumKodu = mahkum.MahkumKodu;
            OturumBilgisi.Instance.MetaDoldur(r);

            var existing = _revirler.FindIndex(x => x.KayitId == r.KayitId);
            if (existing >= 0) { r.GuncellemeZamani = DateTime.Now; _revirler[existing] = r; }
            else _revirler.Add(r);

            SaveJson("revir", $"revir_{r.KayitId}.json", r);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Revir kaydı: {r.MahkumKodu}");
        }

        public void RevirSoftDelete(string kayitId)
        {
            var r = _revirler.FirstOrDefault(x => x.KayitId == kayitId);
            if (r == null) return;
            r.AktifMi = false; r.GuncellemeZamani = DateTime.Now;
            SaveJson("revir", $"revir_{r.KayitId}.json", r);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Revir kaydı pasife alındı: {r.KayitId}");
        }
        #endregion

        #region Kurul CRUD
        public void KurulKarariKaydet(KurulKarari k)
        {
            if (k.Id == 0) k.Id = _kararlar.Any() ? _kararlar.Max(x => x.Id) + 1 : 1;
            var mahkum = MahkumBulById(k.MahkumId);
            if (mahkum != null) k.MahkumKodu = mahkum.MahkumKodu;
            OturumBilgisi.Instance.MetaDoldur(k);

            var existing = _kararlar.FindIndex(x => x.KayitId == k.KayitId);
            if (existing >= 0) { k.GuncellemeZamani = DateTime.Now; _kararlar[existing] = k; }
            else _kararlar.Add(k);

            SaveJson("kurul", $"karar_{k.KayitId}.json", k);
            _log.Islem(OturumBilgisi.Instance.KullaniciAdi, OturumBilgisi.Instance.Rol, $"Kurul kararı: {k.MahkumKodu} - {k.KararTuru}");
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
            karar.GuncellemeZamani = DateTime.Now;
            SaveJson("kurul", $"karar_{karar.KayitId}.json", karar);

            var durumMetin = durum == OnayDurumlari.Onaylandi ? "onayladı" : "reddetti";
            _log.Islem(kullanici, rol, $"Kurul kararı {durumMetin}: {karar.MahkumKodu} - {karar.KararTuru}");
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
            _olaylar.Where(o => o.AktifMi && o.GirenKullanici == kullanici).OrderByDescending(o => o.OlayTarihi).ToList();

        public List<PsikologDegerlendirme> KullaniciPsikologKayitlari(string kullanici) =>
            _psikologlar.Where(p => p.AktifMi && p.GirenKullanici == kullanici).OrderByDescending(p => p.DegerlendirmeTarihi).ToList();

        public List<RevirKaydi> KullaniciRevirKayitlari(string kullanici) =>
            _revirler.Where(r => r.AktifMi && r.GirenKullanici == kullanici).OrderByDescending(r => r.KayitTarihi).ToList();

        public List<BaseKayit> TumKayitlar()
        {
            var hepsi = new List<BaseKayit>();
            hepsi.AddRange(_olaylar.Where(o => o.AktifMi));
            hepsi.AddRange(_psikologlar.Where(p => p.AktifMi));
            hepsi.AddRange(_revirler.Where(r => r.AktifMi));
            hepsi.AddRange(_kararlar.Where(k => k.AktifMi));
            return hepsi.OrderByDescending(k => k.OlusturmaZamani).ToList();
        }
        #endregion

        private void SaveJson<T>(string folder, string fileName, T data)
        {
            var path = Path.Combine(_basePath, folder, fileName);
            File.WriteAllText(path, JsonSerializer.Serialize(data, _jsonOpt));
        }
    }
}
