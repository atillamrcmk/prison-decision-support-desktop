using System;
using System.IO;

namespace KKDS.Services
{
    public class LogServisi
    {
        private static readonly Lazy<LogServisi> _instance = new(() => new LogServisi());
        public static LogServisi Instance => _instance.Value;

        private readonly string _logPath;

        private LogServisi()
        {
            _logPath = Path.Combine(@"C:\Paylasim_KKDS", "logs");
            Directory.CreateDirectory(_logPath);
        }

        public void Bilgi(string kullanici, string rol, string mesaj)
            => Yaz("BİLGİ", kullanici, rol, mesaj);

        public void Islem(string kullanici, string rol, string mesaj)
            => Yaz("İŞLEM", kullanici, rol, mesaj);

        public void Hata(string kullanici, string rol, string mesaj)
            => Yaz("HATA", kullanici, rol, mesaj);

        public void Giris(string kullanici, string rol)
            => Yaz("GİRİŞ", kullanici, rol, "Sisteme giriş yapıldı");

        public void Cikis(string kullanici, string rol)
            => Yaz("ÇIKIŞ", kullanici, rol, "Sistemden çıkış yapıldı");

        private void Yaz(string seviye, string kullanici, string rol, string mesaj)
        {
            try
            {
                var dosya = Path.Combine(_logPath, $"log_{DateTime.Now:yyyyMMdd}.txt");
                var satir = $"[{DateTime.Now:HH:mm:ss}] [{seviye}] [{rol}/{kullanici}] {mesaj}";
                File.AppendAllText(dosya, satir + Environment.NewLine);
            }
            catch { }
        }
    }
}
