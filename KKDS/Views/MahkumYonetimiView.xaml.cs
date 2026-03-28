using System.Windows.Controls;
using System.Windows.Input;
using KKDS.Models;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class MahkumYonetimiView : UserControl
    {
        public MahkumYonetimiView() { InitializeComponent(); }
        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Mahkum m && DataContext is MahkumYonetimiViewModel vm)
                vm.KayitSecCommand.Execute(m);
        }
    }
}
