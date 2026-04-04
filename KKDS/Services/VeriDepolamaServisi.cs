using KKDS.Data;

namespace KKDS.Services
{
    /// <summary>Geriye dönük uyumluluk: tüm veri erişimi JSON deposu üzerinden.</summary>
    public static class VeriDepolamaServisi
    {
        public static JsonDataRepository Instance => JsonDataRepository.Instance;
    }
}
