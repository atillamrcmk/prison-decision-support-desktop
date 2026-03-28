using System.Windows.Controls;
using KKDS.ViewModels;

namespace KKDS.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView() { InitializeComponent(); }
        private void PwdBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
                vm.Sifre = ((PasswordBox)sender).Password;
        }
    }
}
