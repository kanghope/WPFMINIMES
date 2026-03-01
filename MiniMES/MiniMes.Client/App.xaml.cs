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
            services.AddSingleton<IItemService, ItemService>();
            services.AddSingleton<IBomService, BomService>();
            services.AddSingleton<IStockRepository, StockRepository>();

            // [추가] 대시보드 데이터 조회를 위한 전용 서비스 등록
            services.AddSingleton<DashboardService>();


            // 1. 먼저 Repository를 등록해야 합니다. (IWorkOrderRepository 구현체가 무엇인지 확인)
            services.AddSingleton<IWorkOrderRepository, WorkOrderRepository>();
      
            // ★ [추가] SerialDeviceService 등록 ★
            // 이 서비스는 통신 포트를 하나만 점유해야 하므로 반드시 Singleton으로 등록합니다.
            // 2. 그 다음 SerialDeviceService를 등록합니다.
            // Singleton으로 등록되어 프로그램 종료 시까지 단 하나의 포트 통신을 유지합니다.
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
            services.AddTransient<ItemManagementViewModel>();
            services.AddTransient<BomManagementViewModel>();
            services.AddTransient<InventoryViewModel>();

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

            // [추가] 프로그램 시작 시 통신 서비스 미리 호출 (포트 감시 시작 준비)
            // GetRequiredService를 한 번 호출해줘야 Singleton 객체가 생성됩니다.
            var plcService = ServiceProvider?.GetRequiredService<SerialDeviceService>();


            var loginView = ServiceProvider?.GetRequiredService<MiniMes.Client.Views.LoginView>();


            if (loginView?.ShowDialog() == true && UserSession.IsLoggedIn)
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
                        var mainView = ServiceProvider?.GetRequiredService<MainWindow>();
                        mainView?.Show();
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
/*
 1. Dispatcher 계열 (UI 스레드에게 부탁하기)
WPF에서 Dispatcher는 UI 작업을 처리하는 대기열(Queue)입니다.

① Dispatcher.Invoke (동기)
의미: "지금 당장 이 UI를 바꾸고, 다 바꿀 때까지 나는 여기서 기다릴게."

사용 시점: UI가 즉시 업데이트되어야 하고, 그 다음 코드 로직이 업데이트된 UI 상태에 의존할 때 사용합니다.

단점: UI 작업이 오래 걸리면 백그라운드 스레드도 같이 멈춰버립니다.

② Dispatcher.BeginInvoke (비동기)
의미: "이 UI 작업 좀 대기열에 넣어줘. 나는 내 할 일 하러 갈게." (대답을 기다리지 않음)

사용 시점: UI 갱신을 요청만 하고 백그라운드 로직은 멈춤 없이 계속 돌아야 할 때 사용합니다.

특징: DispatcherPriority.Background 같은 우선순위를 정할 수 있어 UI 반응성을 높이는 데 유리합니다.

③ Dispatcher.InvokeAsync (비동기 await 가능)
의미: "이 UI 작업을 예약하고, 완료될 때까지 기다릴 수 있는 티켓(Task)을 줘."

사용 시점: 최신 WPF 라이브러리에서 가장 권장되는 방식입니다. await 키워드와 함께 사용하여 코드를 비동기적으로 깔끔하게 짤 수 있습니다.

④ Dispatcher.InvokeAsync(async () => ...)
의미: "UI 스레드에서 비동기 작업을 실행해줘."

사용 시점: 본인이 질문하신 RefreshDashboard()처럼 내부에서 DB를 비동기로 조회(await)하는 메서드를 UI 스레드에서 호출해야 할 때 사용합니다.

2. Task.Run (무거운 작업 떠넘기기)
⑤ Task.Run(() => ...)
의미: "이 작업은 너무 무거우니 UI 스레드 말고 저기 노는 백그라운드 스레드에서 처리해."

사용 시점: DB 대량 조회, 파일 읽기, PLC 데이터 분석 등 시간이 걸리는 로직을 실행할 때 사용합니다.

중요: 여기서 UI 컨트롤을 직접 건드리면 에러가 납니다. 작업이 끝나면 다시 1번의 Dispatcher를 통해 UI로 돌아와야 합니다.

메서드,       실행 위치,          기다리는가? (Block),     주 사용 용도
Invoke,       UI 스레드,          Yes                      ,간단하고 즉각적인 UI 변경
BeginInvoke,  UI 스레드           ,No                      ,로그 출력 등 UI 반응성이 중요할 때
InvokeAsync,  UI 스레드           ,선택(await)             ,현대적인 WPF 비동기 UI 처리
Task.Run,     백그라운드           ,No                     ,"DB 저장, PLC 데이터 분석 등 무거운 로직"
 */

