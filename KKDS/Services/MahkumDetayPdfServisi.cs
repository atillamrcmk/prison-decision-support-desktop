using System;
using System.Collections.Generic;
using System.Linq;
using KKDS.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KKDS.Services
{
    /// <summary>Mahkum detay özetini A4 PDF olarak üretir (grafikler + son 10 olay).</summary>
    public static class MahkumDetayPdfServisi
    {
        private const int SonOlaySayisi = 10;

        public static void Olustur(string dosyaYolu, MahkumDetayPdfVeri v)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("KKDS — Mahkum detay özeti").SemiBold().FontSize(14).FontColor(Colors.Blue.Medium);
                            r.AutoItem().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(12).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text("Kimlik ve durum").SemiBold().FontSize(11);
                        column.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });
                            Satir(t, "Mahkum kodu", BosDegil(v.Mahkum.MahkumKodu));
                            Satir(t, "Ad soyad", BosDegil(v.Mahkum.AdSoyad));
                            Satir(t, "Blok / koğuş", $"{BosDegil(v.Mahkum.Blok)} / {BosDegil(v.Mahkum.Kogus)}");
                            Satir(t, "Durum", BosDegil(v.Mahkum.Durum));
                            Satir(t, "Kuruma giriş", v.Mahkum.KurumaGirisTarihi.ToString("dd.MM.yyyy"));
                        });

                        column.Item().PaddingTop(4).Text("Risk ve tahmin").SemiBold().FontSize(11);
                        column.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(3); });
                            Satir(t, "Risk skoru", v.Risk.ToplamSkor.ToString("F1"));
                            Satir(t, "Risk seviyesi", BosDegil(v.Risk.Seviye));
                            Satir(t, "Eğilim", BosDegil(v.Analiz.Egilim));
                            Satir(t, "7 gün beklenen olay", v.Tahmin.BeklenenOlaySayisi.ToString("F1"));
                            Satir(t, "Olasılık %", v.Tahmin.Olasilik.ToString("F0"));
                            Satir(t, "En riskli zaman", BosDegil(v.Tahmin.EnRiskliZaman));
                            Satir(t, "Beklenen olay türü", BosDegil(v.Tahmin.BeklenenOlayTuru));
                            Satir(t, "Güven aralığı (olay)", $"{v.Tahmin.AltSinir:F1} — {v.Tahmin.UstSinir:F1}");
                        });

                        if (v.Tahmin.EtkiFactorleri.Count > 0)
                        {
                            column.Item().PaddingTop(4).Text("Tahmine etki eden faktörler").SemiBold().FontSize(10);
                            foreach (var f in v.Tahmin.EtkiFactorleri.Take(8))
                                column.Item().Text("• " + f).FontSize(9);
                        }

                        column.Item().PaddingTop(4).Text("Kurul kararı etkisi").SemiBold().FontSize(11);
                        column.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(3); });
                            var ke = v.Analiz.KararEtkisi;
                            Satir(t, "Son karar türü", BosDegil(ke.SonKararTuru));
                            Satir(t, "Öncesi / sonrası (14 gün)", $"{ke.OncesiOlaySayisi} / {ke.SonrasiOlaySayisi}");
                            Satir(t, "Kısa vadeli olumlu etki", EvetHayir(ke.KisaVadeliEtki));
                            Satir(t, "Kalıcı olumlu etki", EvetHayir(ke.KaliciEtki));
                            Satir(t, "Tekrar yükseliş", EvetHayir(ke.TekrarYukselisi));
                        });

                        if (v.HaftalikTrend.Any())
                        {
                            column.Item().PaddingTop(8).Text("Haftalık olay trendi (renkli grafik)").SemiBold().FontSize(11);
                            column.Item().Element(c => HaftalikSutunGrafik(c, v.HaftalikTrend));
                        }

                        column.Item().PaddingTop(10).Text("Son olaylar ve olay türü dağılımı").SemiBold().FontSize(11);
                        column.Item().Row(row =>
                        {
                            row.Spacing(12);
                            row.RelativeItem(1.1f).Column(left =>
                            {
                                left.Item().Text($"Son {SonOlaySayisi} olay").SemiBold().FontSize(10).FontColor(Colors.Grey.Darken2);
                                left.Item().PaddingTop(4).Table(tbl =>
                                {
                                    tbl.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(62);
                                        c.RelativeColumn(2);
                                        c.ConstantColumn(22);
                                        c.ConstantColumn(44);
                                    });
                                    tbl.Header(h =>
                                    {
                                        h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("Tarih").SemiBold().FontSize(7);
                                        h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("Tür").SemiBold().FontSize(7);
                                        h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("Şidd.").SemiBold().FontSize(7);
                                        h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("Zaman").SemiBold().FontSize(7);
                                    });
                                    foreach (var o in v.Olaylar.Take(SonOlaySayisi))
                                    {
                                        tbl.Cell().Padding(3).Text(o.OlayTarihi.ToString("dd.MM.yy")).FontSize(7);
                                        tbl.Cell().Padding(3).Text(Kisalt(o.OlayTuru, 32)).FontSize(7);
                                        tbl.Cell().Padding(3).Text(o.Siddet.ToString()).FontSize(7);
                                        tbl.Cell().Padding(3).Text(Kisalt(o.ZamanDilimi, 10)).FontSize(7);
                                    }
                                });
                            });
                            row.RelativeItem(1f).Column(right =>
                            {
                                right.Item().Text("Olay türü dağılımı (yatay çubuk)").SemiBold().FontSize(10).FontColor(Colors.Grey.Darken2);
                                right.Item().PaddingTop(4).Element(c => OlayTurDagilimGrafik(c, v.OlayTurDagilimi));
                            });
                        });

                        if (v.SistemOzeti.Count > 0)
                        {
                            column.Item().PaddingTop(6).Text("Sistem özeti").SemiBold().FontSize(10);
                            foreach (var satir in v.SistemOzeti.Take(8))
                                column.Item().Text("• " + satir).FontSize(8);
                        }

                        column.Item().PaddingTop(12).AlignCenter().Text(
                                "Bu belge KKDS uygulamasından üretilmiştir; resmi kayıt yerine geçmez.")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    });

                    page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium))
                        .Text(t =>
                        {
                            t.Span("Sayfa ");
                            t.CurrentPageNumber();
                            t.Span(" / ");
                            t.TotalPages();
                        });
                });
            }).GeneratePdf(dosyaYolu);
        }

        /// <summary>8 haftalık renkli sütun grafiği (yükseklik orantılı).</summary>
        private static void HaftalikSutunGrafik(IContainer container, IReadOnlyList<HaftalikTrend> trend)
        {
            var list = trend.Take(8).ToList();
            if (!list.Any()) return;

            int max = list.Max(x => x.OlaySayisi);
            if (max < 1) max = 1;

            const float plotH = 72f;
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var tr = list[i];
                    var renk = PaletteRenk(i);
                    float oran = tr.OlaySayisi / (float)max;
                    float barH = MathF.Max(4f, plotH * oran);

                    row.RelativeItem().PaddingHorizontal(2).Column(col =>
                    {
                        col.Item().Height(plotH).Column(inner =>
                        {
                            inner.Item().Height(plotH - barH);
                            inner.Item().Height(barH).Background(renk);
                        });
                        col.Item().AlignCenter().Text(tr.Hafta).FontSize(7).FontColor(Colors.Grey.Darken2);
                        col.Item().AlignCenter().Text(tr.OlaySayisi.ToString()).FontSize(8).SemiBold();
                    });
                }
            });
        }

        /// <summary>Renkli yatay çubuk grafiği (en fazla 10 tür).</summary>
        private static void OlayTurDagilimGrafik(IContainer container, IReadOnlyList<OlayTurDagilimSatir> items)
        {
            var list = items.OrderByDescending(x => x.Sayi).Take(10).ToList();
            if (!list.Any())
            {
                container.Text("Dağılım verisi yok.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                return;
            }

            int max = list.Max(x => x.Sayi);
            if (max < 1) max = 1;

            container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(8).Column(col =>
            {
                col.Spacing(5);
                for (int i = 0; i < list.Count; i++)
                {
                    var d = list[i];
                    var renk = PaletteRenk(i);
                    float w = d.Sayi / (float)max;

                    col.Item().Row(r =>
                    {
                        r.ConstantItem(88).Text(Kisalt(d.Tur, 20)).FontSize(8);
                        r.RelativeItem().Height(14).AlignMiddle().Row(bar =>
                        {
                            bar.RelativeItem(w).Height(10).Background(renk);
                            bar.RelativeItem(1f - w).Height(10);
                        });
                        r.ConstantItem(26).AlignRight().Text(d.Sayi.ToString()).FontSize(8).SemiBold().FontColor(Colors.Grey.Darken3);
                    });
                }
            });
        }

        private static Color PaletteRenk(int index)
        {
            Color[] p =
            {
                Colors.Blue.Medium,
                Colors.Purple.Medium,
                Colors.Pink.Medium,
                Colors.Orange.Medium,
                Colors.Yellow.Medium,
                Colors.Green.Medium,
                Colors.Teal.Medium,
                Colors.Indigo.Medium,
                Colors.Red.Medium,
                Colors.Cyan.Medium
            };
            return p[index % p.Length];
        }

        private static void Satir(TableDescriptor t, string etiket, string deger)
        {
            t.Cell().Text(etiket).FontColor(Colors.Grey.Darken1);
            t.Cell().Text(deger);
        }

        private static string BosDegil(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s!;

        private static string EvetHayir(bool b) => b ? "Evet" : "Hayır";

        private static string Kisalt(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "—";
            s = s.Replace('\r', ' ').Replace('\n', ' ');
            return s.Length <= max ? s : s[..(max - 1)] + "…";
        }
    }

    /// <summary>PDF üretimi için ViewModel verisinin özeti.</summary>
    public sealed class MahkumDetayPdfVeri
    {
        public required Mahkum Mahkum { get; init; }
        public required AnalizSonuc Analiz { get; init; }
        public required TahminSonuc Tahmin { get; init; }
        public required RiskSkoru Risk { get; init; }
        public required List<HaftalikTrend> HaftalikTrend { get; init; }
        public required List<OlayTurDagilimSatir> OlayTurDagilimi { get; init; }
        public List<string> SistemOzeti { get; init; } = new();
        public required List<Olay> Olaylar { get; init; }
    }

    public sealed class OlayTurDagilimSatir
    {
        public string Tur { get; init; } = "";
        public int Sayi { get; init; }
    }
}
