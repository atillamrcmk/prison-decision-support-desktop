using System.Windows.Controls;
using System.Windows.Input;
using KKDS.Models;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class Acil112KayitView : UserControl
    {
        public Acil112KayitView() { InitializeComponent(); }

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Acil112CagriKaydi k && DataContext is Acil112KayitViewModel vm)
                vm.KayitSecCommand.Execute(k);
        }
    }
}
