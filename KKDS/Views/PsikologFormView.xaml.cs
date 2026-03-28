using System.Windows.Controls;
using System.Windows.Input;
using KKDS.Models;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class PsikologFormView : UserControl
    {
        public PsikologFormView() { InitializeComponent(); }

        private void GecmisKayitlar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is PsikologDegerlendirme k && DataContext is PsikologFormViewModel vm)
                vm.KayitSecCommand.Execute(k);
        }
    }
}
