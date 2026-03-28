using System.Collections.Generic;

namespace KKDS.Models
{
    public class AnalizSonuc
    {
        public List<HaftalikTrend> HaftalikTrendler { get; set; } = new();
        public string Egilim { get; set; } = "stabil"; // artiyor, azaliyor, stabil
        public bool HizliKotulesme { get; set; }
        public bool AniKirilma { get; set; }
        public OruntuAnalizi Oruntu { get; set; } = new();
        public KararEtkiAnalizi KararEtkisi { get; set; } = new();
        public UyumAnalizi Uyum { get; set; } = new();
        public RiskBoyutlari RiskBoyutlari { get; set; } = new();
        public List<string> SistemOzeti { get; set; } = new();
        public List<string> OncelikNedenleri { get; set; } = new();
    }

    public class HaftalikTrend
    {
        public string Hafta { get; set; } = string.Empty;
        public int OlaySayisi { get; set; }
    }

    public class OruntuAnalizi
    {
        public string EnSikOlayTuru { get; set; } = string.Empty;
        public string EnRiskliZamanDilimi { get; set; } = string.Empty;
        public string EnSikHedef { get; set; } = string.Empty;
        public bool SozdenFizikseleEvrim { get; set; }
        public bool TekrarEdenDongu { get; set; }
        public string TekrarDonguDetay { get; set; } = string.Empty;
    }

    public class KararEtkiAnalizi
    {
        public bool KisaVadeliEtki { get; set; }
        public bool KaliciEtki { get; set; }
        public bool TekrarYukselisi { get; set; }
        public string SonKararTuru { get; set; } = string.Empty;
        public int OncesiOlaySayisi { get; set; }
        public int SonrasiOlaySayisi { get; set; }
    }

    public class UyumAnalizi
    {
        public bool PsikologOlayUyumu { get; set; }
        public bool RevirOlayUyumu { get; set; }
        public bool CiftSinyal { get; set; }
    }

    public class RiskBoyutlari
    {
        public string Davranissal { get; set; } = "dusuk"; // yuksek, orta, dusuk
        public string Psikolojik { get; set; } = "dusuk";
        public string Saglik { get; set; } = "dusuk";
        public string Tekrar { get; set; } = "dusuk";
        public string KurulOnceligi { get; set; } = "dusuk";
    }

    public class GlobalKararEtki
    {
        public string KararTuru { get; set; } = string.Empty;
        public int ToplamKarar { get; set; }
        public double YuzdeDeğişim { get; set; }
        public int OlumluSayi { get; set; }
        public int OlumsuzSayi { get; set; }
        public int EtkisizSayi { get; set; }
    }

    public class TahminSonuc
    {
        public double BeklenenOlaySayisi { get; set; }
        public double Olasilik { get; set; }
        public string EnRiskliZaman { get; set; } = string.Empty;
        public string BeklenenOlayTuru { get; set; } = string.Empty;
        public double AltSinir { get; set; }
        public double UstSinir { get; set; }
        public List<string> EtkiFactorleri { get; set; } = new();
    }

    public class RiskSkoru
    {
        public double ToplamSkor { get; set; }
        public string Seviye { get; set; } = "Düşük";
        public double OlayYogunlugu { get; set; }
        public double SiddetOrtalamasi { get; set; }
        public double PsikologRiski { get; set; }
        public double RevirRiski { get; set; }
        public double TrendArtisi { get; set; }
    }
}
