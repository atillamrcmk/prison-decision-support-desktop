using System.Text.Json.Serialization;

namespace KKDS.Models
{
    public class UygulamaAyarlari
    {
        /// <summary>Boşsa uygulama ilk çalıştırmada %LocalAppData%\KKDS\Veri kullanılır.</summary>
        [JsonPropertyName("veri_klasoru_yolu")]
        public string VeriKlasoruYolu { get; set; } = "";
    }
}
