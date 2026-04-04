using System;
using System.Collections.Generic;
using KKDS.Models;

namespace KKDS.Data
{
    /// <summary>Gelecekte SQLite/PostgreSQL vb. ile değiştirilebilir veri erişim sözleşmesi.</summary>
    public interface IDataRepository
    {
        string VeriKokYolu { get; }

        IReadOnlyList<Mahkum> Mahkumlar { get; }
        IReadOnlyList<Mahkum> TumMahkumlar { get; }
        IReadOnlyList<Olay> Olaylar { get; }
        /// <summary>Aktif disiplin olayları (demo dahil). Gösterge paneli / eğitim verisi için.</summary>
        IReadOnlyList<Olay> OlaylarDahilDemo { get; }
        IReadOnlyList<PsikologDegerlendirme> PsikologDegerlendirmeleri { get; }
        IReadOnlyList<RevirKaydi> RevirKayitlari { get; }
        IReadOnlyList<KurulKarari> KurulKararlari { get; }
        /// <summary>Aktif kurul kararları (demo dahil).</summary>
        IReadOnlyList<KurulKarari> KurulKararlariDahilDemo { get; }
        IReadOnlyList<Kullanici> KullanicilarListesi { get; }

        DateTime SonVeriYuklemeZamani { get; }
        int SonYuklemeOkunanDosyaSayisi { get; }
        int SonYuklemeHataliDosyaSayisi { get; }

        void TumVerileriYukle();
        void KullanicilariYenidenYukle();

        Kullanici? GirisYap(string adi, string sifre);
        bool KullaniciVarMi(string adi);
        void KullaniciKaydet(Kullanici k);
        void KullaniciSil(string kullaniciAdi);

        bool DemoMahkumVarMi();
        void DemoVerisiniTemizle();
        void DemoVerisiYukle();

        void MahkumKaydet(Mahkum m);
        void MahkumSoftDelete(int id);
        Mahkum? MahkumBul(string kod);
        Mahkum? MahkumBulById(int id);
        List<Mahkum> MahkumAra(string arama);
        List<Mahkum> MahkumlariGetir(bool demoDahil, bool pasifDahil);

        void OlayKaydet(Olay o);
        void OlaySoftDelete(string kayitId);

        void PsikologKaydet(PsikologDegerlendirme p);
        void PsikologSoftDelete(string kayitId);

        void RevirKaydet(RevirKaydi r);
        void RevirSoftDelete(string kayitId);

        void KurulKarariKaydet(KurulKarari k);
        void KurulOnayVer(string kararKayitId, string rol, string kullanici, string kullaniciAd, string durum, string yorum);

        List<KurulKarari> BekleyenKararlar();
        List<KurulKarari> RolIcinBekleyenKararlar(string rol);
        List<KurulKarari> OnaylanmisKararlar();
        List<KurulKarari> TumKurulKararlari();

        List<Olay> MahkumOlaylari(int mid);
        List<PsikologDegerlendirme> MahkumPsikolog(int mid);
        List<RevirKaydi> MahkumRevir(int mid);
        List<KurulKarari> MahkumKararlari(int mid);

        List<Olay> KullaniciOlaylari(string kullanici);
        List<PsikologDegerlendirme> KullaniciPsikologKayitlari(string kullanici);
        List<RevirKaydi> KullaniciRevirKayitlari(string kullanici);

        List<BaseKayit> TumKayitlar(bool demoDahil);
    }
}
