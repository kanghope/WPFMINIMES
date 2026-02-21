using Microsoft.Extensions.DependencyInjection;
using MiniMes.Client.ViewModels;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using MiniMES.Infastructure.interfaces;
using MiniMES.Infastructure.Services;
using MiniMES.Infrastructure.Auth;
using System;
using System.Windows;
using System.Windows.Threading;

namespace MiniMes.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 서비스들을 담아두는 바구니(컨테이너)입니다.
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. 서비스 등록 (IAuthService 필수!)
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IWorkOrderService, WorkOrderService>();
            services.AddSingleton<IWorkResultService, WorkResultServicePro>();
            // 1. 먼저 Repository를 등록해야 합니다. (IWorkOrderRepository 구현체가 무엇인지 확인)
            services.AddSingleton<IWorkOrderRepository, WorkOrderRepository>();
            // ★ [추가] SerialDeviceService 등록 ★
            // 이 서비스는 통신 포트를 하나만 점유해야 하므로 반드시 Singleton으로 등록합니다.
            // 2. 그 다음 SerialDeviceService를 등록합니다.
            services.AddSingleton<SerialDeviceService>(sp =>
            {
         
                // ★ 컨테이너(sp)에서 이미 등록된 Repository를 꺼내옵니다.
                var repo = sp.GetRequiredService<IWorkOrderRepository>();

                return new SerialDeviceService(repo);
            });

            // 2. ViewModel 등록
            services.AddTransient<LoginViewModel>();
            services.AddTransient<WorkOrderListViewModel>();
            services.AddTransient<WorkOrderEditViewModel>();
            services.AddTransient<WorkResultListViewModel>();
            services.AddTransient<WorkResultRegisterViewModel>();

            // 3. View(화면) 등록 ★이 부분이 핵심입니다★
            // 반드시 클래스의 전체 경로(Namespace)가 맞는지 확인하세요.
            services.AddTransient<MiniMes.Client.Views.LoginView>();
            services.AddTransient<MainWindow>();
        }

        // 1. App.xaml에서 Startup="OnStartup" 이라고 썼다면 
        //    함수 이름도 똑같이 맞추고, 매개변수도 이벤트 형식을 따라야 합니다.
        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    /*
        //    // 여기서 아까 짰던 시작 로직을 넣습니다.
        //    var viewModel = ServiceProvider?.GetService<WorkOrderListViewModel>();
        //    var listView = new MiniMes.Client.Views.WorkOrderListView();

        //    listView.DataContext = viewModel;
        //    listView.Show();*/
        //    var MainView = new MainWindow();
        //    //var MainView = ServiceProvider?.GetService<MainWindow>();
        //    MainView?.Show();
        //}
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var loginView = ServiceProvider.GetRequiredService<MiniMes.Client.Views.LoginView>();


            if (loginView.ShowDialog() == true && UserSession.IsLoggedIn)
            {
                // Dispatcher를 사용해 UI 스레드가 현재 밀린 작업(로그인 창 닫기 등)을 
                // 모두 처리한 후에 MainWindow를 만들도록 예약합니다.
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var mainView = ServiceProvider.GetRequiredService<MainWindow>();
                        mainView.Show();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"메인화면 실행 오류: {ex.Message}");
                        Shutdown();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}


