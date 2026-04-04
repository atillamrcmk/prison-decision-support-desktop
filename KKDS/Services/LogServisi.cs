using System;
using System.IO;
using System.Linq;
using System.Text;

namespace KKDS.Services
{
    public static class LogIslemTipleri
    {
        public const string GirisBasarili = "GIRIS_BASARILI";
        public const string GirisBasarisiz = "GIRIS_BASARISIZ";
        public const string Cikis = "CIKIS";
        public const string KayitOlustur = "KAYIT_OLUSTUR";
        public const string KayitGuncelle = "KAYIT_GUNCELLE";
        public const string KayitPasif = "KAYIT_PASIF";
        public const string RaporPdf = "RAPOR_PDF";
        public const string Hata = "HATA";
        public const string KurulOnay = "KURUL_ONAY";
        public const string KullaniciKayit = "KULLANICI_KAYIT";
        public const string KullaniciSil = "KULLANICI_SIL";
        public const string DemoTemizle = "DEMO_TEMIZLE";
        public const string DemoYukle = "DEMO_YUKLE";
        public const string AyarDegisiklik = "AYAR_DEGISIKLIK";
    }

    public class LogServisi
    {
        private static readonly Lazy<LogServisi> _instance = new(() => new LogServisi());
        public static LogServisi Instance => _instance.Value;

        private string LogKok => Path.Combine(UygulamaAyarlariServisi.Instance.VeriKlasoruYolu, "logs");

        private LogServisi()
        {
            try { Directory.CreateDirectory(LogKok); } catch { }
        }

        public void Bilgi(string kullanici, string rol, string mesaj)
            => KritikIslem("BILGI", kullanici, rol, null, null, mesaj);

        public void Islem(string kullanici, string rol, string mesaj)
            => KritikIslem("ISLEM", kullanici, rol, null, null, mesaj);

        public void Hata(string kullanici, string rol, string mesaj)
            => KritikIslem(LogIslemTipleri.Hata, kullanici, rol, null, null, mesaj);

        public void Giris(string kullanici, string rol)
            => KritikIslem(LogIslemTipleri.GirisBasarili, kullanici, rol, "oturum", null, "Sisteme giriş yapıldı");

        public void GirisBasarisiz(string kullaniciDenenen, string sebep)
            => KritikIslem(LogIslemTipleri.GirisBasarisiz, kullaniciDenenen ?? "-", "-", "oturum", null, sebep);

        public void Cikis(string kullanici, string rol)
            => KritikIslem(LogIslemTipleri.Cikis, kullanici, rol, "oturum", null, "Sistemden çıkış");

        public void KritikIslem(string islemTipi, string kullanici, string rol, string? hedefKayitTuru, string? mahkumKodu, string mesaj)
        {
            try
            {
                Directory.CreateDirectory(LogKok);
                var dosya = Path.Combine(LogKok, $"audit_{DateTime.Now:yyyyMMdd}.log");
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.Append(" | ");
                sb.Append(islemTipi);
                sb.Append(" | kullanici=");
                sb.Append(kullanici);
                sb.Append(" | rol=");
                sb.Append(rol);
                sb.Append(" | hedef=");
                sb.Append(hedefKayitTuru ?? "-");
                sb.Append(" | mahkum=");
                sb.Append(mahkumKodu ?? "-");
                sb.Append(" | ");
                sb.Append(mesaj);
                File.AppendAllText(dosya, sb + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>audit_*.log dosyalarının son satırları (basit okuma).</summary>
        public string SonAuditSatirlari(int maxSatir = 500)
        {
            try
            {
                Directory.CreateDirectory(LogKok);
                var dosyalar = Directory.GetFiles(LogKok, "audit_*.log");
                Array.Sort(dosyalar, StringComparer.Ordinal);
                var sb = new StringBuilder();
                foreach (var d in dosyalar.Reverse().Take(3))
                {
                    sb.AppendLine($"=== {Path.GetFileName(d)} ===");
                    var satirlar = File.ReadAllLines(d, Encoding.UTF8);
                    foreach (var line in satirlar.TakeLast(maxSatir))
                        sb.AppendLine(line);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Log okunamadı: " + ex.Message;
            }
        }

        public string EskiMetinLoglariOku()
        {
            try
            {
                var sb = new StringBuilder();
                Directory.CreateDirectory(LogKok);
                foreach (var d in Directory.GetFiles(LogKok, "log_*.txt").OrderByDescending(x => x))
                {
                    sb.AppendLine($"=== {Path.GetFileName(d)} ===");
                    sb.AppendLine(File.ReadAllText(d, Encoding.UTF8));
                }
                return sb.ToString();
            }
            catch { return ""; }
        }
    }
}
