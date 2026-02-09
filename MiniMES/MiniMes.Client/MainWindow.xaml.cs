using Microsoft.Extensions.DependencyInjection;
using MiniMes.Client.ViewModels;
using MiniMes.Client.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniMes.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnOpenWorkOrderClick(object sender, RoutedEventArgs e)
        {
            // 1. 표시할 화면(UserControl)과 그에 맞는 뷰모델을 준비합니다.
            // (실무에서는 DI 컨테이너를 써서 가져오지만, 여기서는 이해를 위해 직접 생성합니다.)
            var view = new WorkOrderListView();
            // 2. 만약 기존에 Window에서 쓰던 ViewModel을 그대로 쓰고 싶다면 꽂아줍니다.
            // view.DataContext = new WorkOrderListViewModel(...);

            // 2. DI 바구니(ServiceProvider)에서 뷰모델을 꺼내서 꽂아줌
            // 이렇게 꺼내야 IWorkOrderService 같은 부품들이 자동으로 조립된 뷰모델이 나옵니다.
            view.DataContext = App.ServiceProvider?.GetService<WorkOrderListViewModel>();

            // 3. 메인 창의 ContentControl에 표시
            MainContent.Content = view;

        }
    }
}