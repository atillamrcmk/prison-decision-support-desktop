using KKDS.Models;

namespace KKDS.Services
{
    public class OturumBilgisi
    {
        private static readonly Lazy<OturumBilgisi> _instance = new(() => new OturumBilgisi());
        public static OturumBilgisi Instance => _instance.Value;

        public Kullanici? AktifKullanici { get; set; }

        public string KullaniciAdi => AktifKullanici?.KullaniciAdi ?? "";
        public string Rol => AktifKullanici?.Rol ?? "";
        public string AdSoyad => AktifKullanici?.AdSoyad ?? "";

        public void MetaDoldur(BaseKayit kayit)
        {
            kayit.GirenKullanici = KullaniciAdi;
            kayit.GirenRol = Rol;
        }
    }
}
