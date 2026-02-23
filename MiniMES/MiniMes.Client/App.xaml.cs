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
            /*
            구분          ,AddSingleton,                                    AddTransient
            생성          횟수,단 1번(최초 요청 시)                         ,요청할 때마다 매번
            데이터 유지,  프로그램 종료 시까지 유지됨,                      요청이 끝나면 초기화됨
            메모리 효율,  "하나만 쓰므로 메모리 절약 (단, 계속 점유)"       ,쓰고 버리므로 누적 메모리 부하 적음
            주요 대상,    "SerialDeviceService, AuthService"                ,"LoginView, WorkOrderEditViewModel"
            */
            //AddSingleton, AddTransient 객체를 얼마나 자주 새로 만드느냐(수명주기) 차이
            // 1. 서비스 등록 (IAuthService 필수!)
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IWorkOrderService, WorkOrderService>();
            services.AddSingleton<IWorkResultService, WorkResultServicePro>();
            services.AddSingleton<IDashboardService, DashboardService>();
            // [추가] 대시보드 데이터 조회를 위한 전용 서비스 등록
            services.AddSingleton<DashboardService>();


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

            // [추가] 대시보드 뷰모델 등록 (전역 PlcService를 주입받음)
            services.AddTransient<DashboardViewModel>();

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
                //보조 작업자가 메인 작업자(UI 스레드)에게 '이것 좀 화면에 그려줘'
                //Dispatcher.BeginInvoke** 를 사용해서 메인 스레드에게 업무를 전달
                //Dispatcher: 메인 스레드가 할 일들을 쌓아두는 **'업무 대기열(Queue)'**입니다.
                //CurrentDispatcher: 현재 이 코드가 속한 스레드의 관리자를 찾습니다.
                //BeginInvoke: "이 일 좀 해줘"라고 부탁하고, 보조 스레드는 자기 할 일을 하러 바로 떠납니다(비동기). (대답을 기다리지 않습니다.)
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
                //급한 일(화면 멈춤 방지)부터 처리하고, 이 작업은 조금 이따가 여유 있을 때 해줘"**라고 우선순위를 정해주는 것
                /*
                 * 장비 신호 폭주: PLC에서 데이터가 미친 듯이 들어옵니다.
                    DispatcherPriority.Background 사용: 데이터가 들어올 때마다 화면 숫자를 바꾸라는 명령을 Background로 던집니다.
                    결과: 컴퓨터는 사용자가 버튼을 누르는 동작을 최우선으로 처리하고, 남는 시간에 화면의 생산 수량을 업데이트합니다. 
                    덕분에 데이터는 실시간으로 반영되면서도 마우스 조작은 부드럽게 유지됩니다.
                    Background 파라미터는 **"UI의 반응성(Responsiveness)을 보장하기 위한 안전장치"**입니다. 
                    사용자 경험을 해치지 않으면서 백그라운드 데이터를 화면에 뿌리고 싶을 때 사용하는 실무적인 기법입니다.
                 */
            }
        }

        // [추가] 프로그램 종료 시 처리
        protected override void OnExit(ExitEventArgs e)
        {
            // 프로그램이 꺼질 때 열려있는 시리얼 포트를 안전하게 닫습니다.
            var plcService = ServiceProvider?.GetService<SerialDeviceService>();
            plcService?.Dispose();

            base.OnExit(e);
        }
    }
}


