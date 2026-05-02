using System;
using System.Collections.Generic;
using System.Linq;
using KKDS.Data;
using KKDS.Models;

namespace KKDS.Services
{
    public static class KurumKogusAnalizServisi
    {
        public static (KurumGenelAnalizOzet genel, List<KogusAnalizSatir> koguslar, List<BlokAnalizSatir> bloklar) Hesapla(
            IDataRepository veri,
            IReadOnlyDictionary<int, RiskSkoru> riskler,
            DateTime? referansTarihi = null)
        {
            var bugun = (referansTarihi ?? DateTime.Today).Date;
            var mahkumlar = veri.MahkumlariGetir(demoDahil: true, pasifDahil: false).ToList();

            var olaylar30 = veri.OlaylarDahilDemo.Where(o => o.OlayTarihi >= bugun.AddDays(-30)).ToList();
            var kararlar30 = veri.KurulKararlariDahilDemo.Where(k => k.KararTarihi >= bugun.AddDays(-30)).ToList();

            var genel = new KurumGenelAnalizOzet
            {
                ToplamMahkum = mahkumlar.Count,
                Son30GunOlay = olaylar30.Count,
                Son30GunKarar = kararlar30.Count,
                YakinIzlemSayisi = mahkumlar.Count(m => m.Durum == "yakin_izlem")
            };

            if (mahkumlar.Count == 0)
            {
                genel.OzetMetin = "Kayıtlı aktif mahkum bulunmuyor; koğuş analizi için önce mahkum kaydı girin.";
                return (genel, new List<KogusAnalizSatir>(), new List<BlokAnalizSatir>());
            }

            genel.OrtalamaRisk = Math.Round(mahkumlar.Average(m => riskler.TryGetValue(m.Id, out var r) ? r.ToplamSkor : 0), 1);
            var enRiskli = mahkumlar.OrderByDescending(m => riskler.TryGetValue(m.Id, out var r) ? r.ToplamSkor : 0).First();
            genel.EnYuksekRiskKod = enRiskli.MahkumKodu;
            genel.EnYuksekRiskDeger = riskler.TryGetValue(enRiskli.Id, out var er) ? er.ToplamSkor : 0;

            var kogusGruplar = mahkumlar
                .GroupBy(m => (
                    Blok: string.IsNullOrWhiteSpace(m.Blok) ? "(Blok belirtilmemiş)" : m.Blok.Trim(),
                    Kogus: string.IsNullOrWhiteSpace(m.Kogus) ? "(Koğuş belirtilmemiş)" : m.Kogus.Trim()))
                .ToList();

            genel.KogusSayisi = kogusGruplar.Count;
            genel.BlokSayisi = mahkumlar
                .Select(m => string.IsNullOrWhiteSpace(m.Blok) ? "(Blok belirtilmemiş)" : m.Blok.Trim())
                .Distinct()
                .Count();

            var koguslar = new List<KogusAnalizSatir>();
            foreach (var g in kogusGruplar)
            {
                var ids = g.Select(m => m.Id).ToHashSet();
                var olayAdet = olaylar30.Count(o => ids.Contains(o.MahkumId));
                var kararAdet = kararlar30.Count(k => ids.Contains(k.MahkumId));
                var yakin = g.Count(m => m.Durum == "yakin_izlem");

                double sumR = 0;
                var maxR = double.MinValue;
                Mahkum? enRMahkum = null;
                foreach (var m in g)
                {
                    var sk = riskler.TryGetValue(m.Id, out var rr) ? rr.ToplamSkor : 0;
                    sumR += sk;
                    if (sk > maxR)
                    {
                        maxR = sk;
                        enRMahkum = m;
                    }
                }
                if (maxR == double.MinValue) maxR = 0;
                var ortR = Math.Round(sumR / g.Count(), 1);

                var satir = new KogusAnalizSatir
                {
                    Blok = g.Key.Blok,
                    Kogus = g.Key.Kogus,
                    MahkumSayisi = g.Count(),
                    Son30GunOlay = olayAdet,
                    Son30GunKarar = kararAdet,
                    OlayBasiMahkum = g.Count() > 0 ? Math.Round(olayAdet / (double)g.Count(), 2) : 0,
                    OrtalamaRisk = ortR,
                    EnYuksekRisk = maxR,
                    EnRiskliMahkumKodu = enRMahkum?.MahkumKodu ?? "-",
                    EnRiskliMahkumId = enRMahkum?.Id ?? 0,
                    YakinIzlemSayisi = yakin
                };
                koguslar.Add(satir);
            }

            koguslar = koguslar
                .OrderByDescending(x => x.Son30GunOlay)
                .ThenByDescending(x => x.OrtalamaRisk)
                .ThenBy(x => x.Blok)
                .ThenBy(x => x.Kogus)
                .ToList();

            var enOlay = koguslar.OrderByDescending(k => k.Son30GunOlay).FirstOrDefault();
            if (enOlay != null && enOlay.Son30GunOlay > 0)
            {
                genel.EnCokOlayKogusu = $"{enOlay.Blok} · {enOlay.Kogus}";
                genel.EnCokOlayKogusuAdet = enOlay.Son30GunOlay;
            }

            var enOrtRisk = koguslar.Where(k => k.MahkumSayisi > 0).OrderByDescending(k => k.OrtalamaRisk).FirstOrDefault();
            if (enOrtRisk != null)
            {
                genel.EnYuksekOrtRiskKogusu = $"{enOrtRisk.Blok} · {enOrtRisk.Kogus}";
                genel.EnYuksekOrtRiskDeger = enOrtRisk.OrtalamaRisk;
            }

            genel.OzetMetin =
                $"Kurumda {genel.ToplamMahkum} aktif mahkum, {genel.BlokSayisi} blok ve {genel.KogusSayisi} farklı koğuş kaydı üzerinden izleniyor. " +
                $"Son 30 günde toplam {genel.Son30GunOlay} disiplin olayı ve {genel.Son30GunKarar} kurul kararı işlendi; " +
                $"yakın izlemde {genel.YakinIzlemSayisi} kişi var. Mahkum başına ortalama risk skoru {genel.OrtalamaRisk:F1}.";

            var bloklar = mahkumlar
                .GroupBy(m => string.IsNullOrWhiteSpace(m.Blok) ? "(Blok belirtilmemiş)" : m.Blok.Trim())
                .Select(bg =>
                {
                    var ids = bg.Select(m => m.Id).ToHashSet();
                    var kogusAdet = bg.GroupBy(m => string.IsNullOrWhiteSpace(m.Kogus) ? "(Koğuş belirtilmemiş)" : m.Kogus.Trim()).Count();
                    var o30 = olaylar30.Count(o => ids.Contains(o.MahkumId));
                    var k30 = kararlar30.Count(k => ids.Contains(k.MahkumId));
                    var ort = Math.Round(bg.Average(m => riskler.TryGetValue(m.Id, out var r) ? r.ToplamSkor : 0), 1);
                    return new BlokAnalizSatir
                    {
                        Blok = bg.Key,
                        MahkumSayisi = bg.Count(),
                        KogusSayisi = kogusAdet,
                        Son30GunOlay = o30,
                        Son30GunKarar = k30,
                        OrtalamaRisk = ort
                    };
                })
                .OrderByDescending(b => b.Son30GunOlay)
                .ThenByDescending(b => b.OrtalamaRisk)
                .ToList();

            return (genel, koguslar, bloklar);
        }
    }
}
