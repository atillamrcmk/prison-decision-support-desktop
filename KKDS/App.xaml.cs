using System.Windows;
using QuestPDF;
using QuestPDF.Infrastructure;
using KKDS.Services;

namespace KKDS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            UygulamaAyarlariServisi.Instance.Yukle();
            base.OnStartup(e);
        }
    }
}
