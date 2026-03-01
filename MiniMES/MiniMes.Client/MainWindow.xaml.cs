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
using MiniMES.Infrastructure.Auth; // UserSession이 있는 네임스페이스

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

            // [추가] 창이 열릴 때 로그인 정보를 화면에 표시
            LoadUserInfo();

            // [추가] 처음 창이 뜰 때 기본 화면으로 대시보드를 보여줍니다.
            OnOpenDashboardClick(null, null);
        }
        private void LoadUserInfo()
        {
            // UserSession에 저장된 정보를 UI 컨트롤에 직접 대입
            TxtUserName.Text = $"{UserSession.UserName ?? "미로그인"} 님";
            TxtUserRole.Text = UserSession.UserRole == "ADMIN" ? "시스템 관리자" : "일반 작업자";
        }
        // [추가] 실시간 대시보드 화면 열기
        private void OnOpenDashboardClick(object sender, RoutedEventArgs e)
        {
            // 1. DI 컨테이너에서 뷰모델을 먼저 가져옵니다 (PLC 서비스 등이 주입된 상태)
            var viewModel = App.ServiceProvider?.GetRequiredService<DashboardViewModel>();
            // 2. 대시보드 뷰를 생성합니다.
            var view = new DashboardView();
            // 3. 뷰와 뷰모델을 연결합니다.
            view.DataContext = App.ServiceProvider?.GetService<DashboardViewModel>();
            // 4. 메인 영역에 표시합니다.
            MainContent.Content = view;


        }
        //로그아웃
        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("로그아웃 하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // 1. 현재 세션 정보 클리어 (선택 사항: UserSession에 Clear 메서드가 있다면 호출)
                UserSession.Clear();

                // 2. DI 컨테이너에서 새로운 로그인 창을 가져옵니다.
                var loginView = App.ServiceProvider?.GetRequiredService<MiniMes.Client.Views.LoginView>();

                if (loginView != null)
                {
                    // 3. 새 로그인 창을 화면에 표시합니다.
                    loginView.Show();

                    // 4. 현재 메인 창(MainWindow)을 닫습니다.
                    this.Close();
                }
            }
        }
        // 작업 지시 관리 화면 열기
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
        // 품목 마스터 관리 화면 열기
        private void OnOpenItemMasterClick(object sender, RoutedEventArgs e)
        {
            var view = new ItemManagementView();

            view.DataContext = App.ServiceProvider?.GetService<ItemManagementViewModel>();

            MainContent.Content = view;

        }
        // 품목 마스터 관리 화면 열기
        private void OnOpenBomClick(object sender, RoutedEventArgs e)
        {
            var view = new BomManagementView();

            view.DataContext = App.ServiceProvider?.GetService<BomManagementViewModel>();

            MainContent.Content = view;

        }
        // 입고 관리 화면 열기
        private void OnOpenInventoryViewClick(object sender, RoutedEventArgs e)
        {
            var view = new InventoryView();

            view.DataContext = App.ServiceProvider?.GetService<InventoryViewModel>();

            MainContent.Content = view;
        }
    }
}

/*
 --추가적으로 구현해야할 항목들 
현재 사용자님이 구축하신 구조(DI, MVVM, Service Layer 분리)는 이미 실무의 **'핵심 뼈대'**를 아주 잘 갖추고 있습니다. 여기서 "포트폴리오용 토이 프로젝트"를 넘어 진짜 "현장에서 돌아가는 시스템" 느낌을 주려면, 단순 CRUD 외에 MES 특유의 복잡한 비즈니스 로직이 추가되어야 합니다.

실무 MES 전문가들이 중요하게 보는 4가지 확장 포인트를 제안해 드립니다.

1. 생산 현황 대시보드 (Dashboard)
실무 MES의 첫 화면은 리스트가 아니라 공장 전체의 현황을 한눈에 보는 대시보드입니다.

가동률/비가동률: 현재 가동 중인 설비와 멈춘 설비를 원형 차트로 표시.

실시간 목표 달성률 (KPI): 지시 수량 대비 현재 양품 생산량을 게이지 차트로 시각화.

알람 현황: 최근 발생한 불량이나 설비 장애 이력을 실시간 리스트로 노출.

2. 기준 정보 관리 (Master Data)의 심화
지금은 작업 지시만 있지만, 이를 뒷받침하는 기준 정보가 탄탄해야 실무 느낌이 납니다.

BOM (Bill of Material) 관리: 제품 하나를 만들기 위해 어떤 원부자재가 몇 개 들어가는지 트리 구조로 관리.

라우팅(Routing) 관리: 제품이 어떤 공정(절단 -> 용접 -> 도장 -> 조립)을 거치는지 순서와 표준 공수(시간) 정의.

설비/금형 관리: 작업이 이루어지는 기계 장치의 사양과 정기 점검 이력 관리.

3. 자재/재고 연동 (Material Management)
실무 MES는 생산만 하지 않습니다. 생산하면 재고가 줄어드는 로직이 핵심입니다.

자재 투입 (Backflush): 작업 실적을 등록하는 순간, BOM에 정의된 만큼 자재 창고의 재고가 자동으로 차감되는 로직.

LOT 추적 (Traceability): "이 제품에 들어간 나사는 언제 입고된 어느 업체 것인가?"를 추적하는 역추적 기능.

재고 실사: 시스템 재고와 실제 창고 재고를 맞추는 조정 기능.

4. 공정간 이동 및 물류 제어
하나의 지시가 끝났을 때 다음 공정으로 어떻게 넘어가는지를 구현하면 매우 전문적으로 보입니다.

창고 이동 (Move Transaction): 1공정 양품이 2공정의 '원재료'로 입고되는 흐름.

비가동 관리 (Downtime): "왜 작업 시작을 못 하고 있는가?" (자재 부족, 설비 고장, 회의 등)를 기록하는 기능.

Label 발행: 실적 등록 시 제품에 붙일 바코드 라벨을 출력하는 기능 (가상으로 출력 로그만 남겨도 좋습니다).

5. 기술적 완성도 (실무적 디테일)
코드 레벨에서 '실무자'들이 환호할 만한 디테일입니다.

로그 관리 (Logging): 누가 언제 어떤 데이터를 수정했는지 기록하는 Audit Trail. (NLog나 Serilog 사용)

권한 관리: 관리자(지시 생성 가능)와 작업자(실적 등록만 가능)의 메뉴 접근 권한 분리.

비동기 최적화: 모든 DB 작업에 CancellationToken을 적용하여 사용자가 긴 작업을 취소할 수 있게 배려.


**PLC 연동, 
실무에서 PLC 연동은 보통 Modbus RTU(시리얼) 
또는 Modbus TCP(이더넷) 프로토콜을 가장 많이 사용합니다.
현재 환경(com0com, Hercules)을 고려하면 
Modbus RTU 시뮬레이션 방식으로 접근하는 것이 
가장 현실적이고 실무에 가깝습니다.

**리포트 출력
 */