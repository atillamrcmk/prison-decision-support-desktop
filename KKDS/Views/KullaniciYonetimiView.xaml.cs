using System.Windows.Controls;
using System.Windows.Input;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class KullaniciYonetimiView : UserControl
    {
        public KullaniciYonetimiView() { InitializeComponent(); }

        private void Kullanicilar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is KullaniciSatir s && DataContext is KullaniciYonetimiViewModel vm)
            {
                vm.SecCommand.Execute(s);
                PwdBox.Clear();
                PwdBox2.Clear();
            }
        }

        private void PwdBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is KullaniciYonetimiViewModel vm)
                vm.Sifre = ((PasswordBox)sender).Password;
        }

        private void PwdBox2_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is KullaniciYonetimiViewModel vm)
                vm.SifreTekrar = ((PasswordBox)sender).Password;
        }
    }
}
