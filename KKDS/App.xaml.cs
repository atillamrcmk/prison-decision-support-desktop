using System.Windows;
using QuestPDF.Infrastructure;

namespace KKDS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            base.OnStartup(e);
        }
    }
}
