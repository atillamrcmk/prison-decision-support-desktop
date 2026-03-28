using System.Windows.Controls;
using System.Windows.Input;
using KKDS.Models;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class DisiplinFormView : UserControl
    {
        public DisiplinFormView() { InitializeComponent(); }
        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Olay k && DataContext is DisiplinFormViewModel vm)
                vm.KayitSecCommand.Execute(k);
        }
    }
}
