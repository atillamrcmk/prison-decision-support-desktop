using System.Windows.Controls;
using System.Windows.Input;
using KKDS.Models;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class RevirFormView : UserControl
    {
        public RevirFormView() { InitializeComponent(); }
        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is RevirKaydi k && DataContext is RevirFormViewModel vm)
                vm.KayitSecCommand.Execute(k);
        }
    }
}
