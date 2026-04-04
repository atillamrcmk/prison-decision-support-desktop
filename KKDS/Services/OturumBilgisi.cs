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

        /// <summary>Yeni kayıt oluştururken veya ilk kayıtta çağrılır.</summary>
        public void MetaDoldurYeni(BaseKayit kayit)
        {
            var u = KullaniciAdi;
            var r = Rol;
            kayit.GirenKullanici = u;
            kayit.GirenRol = r;
            kayit.OlusturanKullanici = u;
            kayit.GuncelleyenKullanici = u;
            kayit.OlusturmaZamani = System.DateTime.Now;
            kayit.GuncellemeZamani = System.DateTime.Now;
        }

        /// <summary>Mevcut kayıt güncellenirken veya pasife alınırken.</summary>
        public void MetaGuncelle(BaseKayit kayit)
        {
            var u = KullaniciAdi;
            var r = Rol;
            kayit.GirenKullanici = u;
            kayit.GirenRol = r;
            kayit.GuncelleyenKullanici = u;
            kayit.GuncellemeZamani = System.DateTime.Now;
        }
    }
}
