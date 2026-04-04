using System;
using System.IO;
using System.Text.Json;
using KKDS.Models;

namespace KKDS.Services
{
    public class UygulamaAyarlariServisi
    {
        private static readonly Lazy<UygulamaAyarlariServisi> _instance = new(() => new UygulamaAyarlariServisi());
        public static UygulamaAyarlariServisi Instance => _instance.Value;

        private readonly string _ayarDosyaYolu;
        private readonly JsonSerializerOptions _jsonOpt = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private UygulamaAyarlariServisi()
        {
            var baseDir = AppContext.BaseDirectory;
            _ayarDosyaYolu = Path.Combine(baseDir, "appsettings.json");
        }

        public UygulamaAyarlari Ayarlar { get; private set; } = new();

        public string VeriKlasoruYolu => string.IsNullOrWhiteSpace(Ayarlar.VeriKlasoruYolu)
            ? VarsayilanVeriYolu()
            : Ayarlar.VeriKlasoruYolu.Trim();

        public static string VarsayilanVeriYolu()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(local, "KKDS", "Veri");
        }

        public void Yukle()
        {
            try
            {
                if (File.Exists(_ayarDosyaYolu))
                {
                    var json = File.ReadAllText(_ayarDosyaYolu);
                    var a = JsonSerializer.Deserialize<UygulamaAyarlari>(json, _jsonOpt);
                    if (a != null) Ayarlar = a;
                }
                else
                {
                    Ayarlar = new UygulamaAyarlari { VeriKlasoruYolu = "" };
                    Kaydet();
                }
            }
            catch
            {
                Ayarlar = new UygulamaAyarlari();
            }
        }

        public void Kaydet()
        {
            try
            {
                var json = JsonSerializer.Serialize(Ayarlar, _jsonOpt);
                File.WriteAllText(_ayarDosyaYolu, json);
            }
            catch { /* ayar yazılamazsa varsayılan yol kullanılmaya devam */ }
        }

        public void VeriYolunuAyarla(string tamYol)
        {
            Ayarlar.VeriKlasoruYolu = tamYol ?? "";
            Kaydet();
        }
    }
}
