using System.Windows.Controls;
using System.Windows.Input;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class KurulOnaylarView : UserControl
    {
        public KurulOnaylarView() { InitializeComponent(); }

        private void Kararlar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is OnayKararSatir satir && DataContext is KurulOnaylarViewModel vm)
                vm.KararSecCommand.Execute(satir);
        }
    }
}
