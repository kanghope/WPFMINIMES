using MiniMes.Client.ViewModels;
using MiniMES.Infrastructure.Auth;
using MiniMes.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MiniMes.Client.Views
{
    /// <summary>
    /// LoginView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoginView : Window
    {
        // 생성자 주입 방식 (App.xaml.cs에서 DI 컨테이너를 쓰거나 직접 주입)
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        // PasswordBox는 보안상 바인딩이 바로 안 되므로 간단한 넘겨주기 로직이 필요할 수 있습니다.
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is LoginViewModel vm)
            {
                vm.Password = ((System.Windows.Controls.PasswordBox)sender).Password;
            }
        }

        // 창 닫기 버튼 전용 (DI를 사용하므로 Application.Current.Shutdown 대신 Close 사용)
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
