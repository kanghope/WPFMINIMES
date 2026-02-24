using MiniMes.Client;
using MiniMes.Client.Helpers;
using MiniMes.Client.ViewModels;
using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using MiniMES.Infastructure.interfaces;
using MiniMES.Infastructure.Services;
using MiniMES.Infrastructure.Auth;
using System;
using System.Collections.ObjectModel; // UI와 실시간으로 연동되는 리스트를 쓰기 위함
using System.ComponentModel;          // "데이터 바뀌었으니 화면 새로 그려!"라고 알려주는 기능
using System.ComponentModel.Design;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;         // 비동기(딴짓하면서 일하기)를 위한 도구
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;           // 버튼 클릭 같은 명령을 처리하기 위함
using System.Windows.Media;

namespace MiniMes.Client.ViewModels
{
    
    // INotifyPropertyChanged: "내 데이터가 바뀌면 화면에 알려주겠다"는 약속입니다.
    public class WorkOrderListViewModel : BaseViewModel
    {
        /*1. 왜 실무에서는 직접 new를 하지 않을까요?
        부품 갈아끼우기 편리함: 나중에 Oracle DB에서 MySQL로 바꾸거나, 진짜 PLC 대신 가상 PLC 서비스로 바꾸고 싶을 때, 모든 코드를 고칠 필요 없이 Program.cs에서 딱 한 줄만 수정하면 됩니다.
        성능 관리: DB 연결 객체(DbContext) 등을 매번 만들지 않고 하나로 돌려쓰거나(Singleton), 필요할 때만 깔끔하게 생성해서 관리하기 좋습니다.
        테스트 용이성: 가짜(Mock) 데이터를 보내주는 테스트용 서비스를 쉽게 연결할 수 있습니다.
        // [1. 도구들] DB와 통신하거나 데이터를 가져오는 서비스 객체들입니다.*/
        //private readonly WorkOrderService _service = new WorkOrderService();
        private readonly IWorkOrderService _service;//
        //private readonly WorkResultService _WorkResultsService = new WorkResultService();
        private readonly IWorkResultService _WorkResultsService;

        private readonly IWorkOrderRepository _workOrderRepository;

        private readonly SerialDeviceService _serialDeviceService;

        // [추가] 연속 클릭 시 이전 비동기 작업을 취소하기 위한 소스
        private CancellationTokenSource? _loadCts;

        // [2. 데이터 저장소] 화면에 보여줄 실제 값들입니다.

        // _workOrders: 실제 데이터가 담긴 주머니입니다.
        private ObservableCollection<WorkOrderDto> _workOrders = new ObservableCollection<WorkOrderDto>();
        
       

        // WorkOrders: 화면(DataGrid)이 이 이름을 보고 데이터를 가져갑니다.
        public ObservableCollection<WorkOrderDto> WorkOrders
        {
            get => _workOrders;
            set { _workOrders = value; OnPropertyChanged(nameof(WorkOrders)); }
        }

        // [실시간 모니터링] 통신 로그 및 상태 프로퍼티
        public ObservableCollection<string> CommunicationLogs { get; } = new ObservableCollection<string>();

        private string _deviceStatusText = "연결 대기 중";
        public string DeviceStatusText { get => _deviceStatusText; set { _deviceStatusText = value; OnPropertyChanged(nameof(DeviceStatusText)); } }

        private Brush _deviceStatusColor = Brushes.Gray;
        public Brush DeviceStatusColor { get => _deviceStatusColor; set { _deviceStatusColor = value; OnPropertyChanged(nameof(DeviceStatusColor)); } }

        private string _lastSignalTime = "-";
        public string LastSignalTime { get => _lastSignalTime; set { _lastSignalTime = value; OnPropertyChanged(nameof(LastSignalTime)); } }

        private int _packetCount = 0;
        public int PacketCount { get => _packetCount; set { _packetCount = value; OnPropertyChanged(nameof(PacketCount)); } }

        //진행중 콤보박스 리스트(객체목록)
        public ObservableCollection<StatusItem> StatusOptions { get; set; } = new ObservableCollection<StatusItem>
        {
            new StatusItem {DisplayName = "전체", StatusCode = "ALL"},
            new StatusItem {DisplayName = "대기", StatusCode = "W"},
            new StatusItem {DisplayName = "진행중", StatusCode = "P"},
            new StatusItem {DisplayName = "완료", StatusCode = "C"}
        };

        // 2. 사용자가 선택한 '값'을 담을 변수 (예: "W", "P", "C"가 담김)
        private string? _selectedStatusCode;
        public string SelectedStatusCode
        {
            get => _selectedStatusCode;
            set
            {
                if(_selectedStatusCode != value)
                {
                    _selectedStatusCode = value;
                    OnPropertyChanged(nameof(SelectedStatusCode));
                    // 여기서 DB 조회를 하면 "W" 같은 코드로 검색할 수 있습니다.

                    // [핵심] 값이 바뀌는 순간, 비동기로 조회를 시작합니다.
                    // _ = 는 결과를 기다리지 않고(Fire and Forget) 실행한다는 뜻입니다.
                    // _service가 주입되지 않았을 때는 실행하지 않도록 보호
                    //if (_service != null)
                    //{
                    //    _ = ExecuteLoadCommandAsync();
                    //}
                }
            }
        }

        // SelectedWorkOrder: 마우스로 클릭한 '그 줄'의 정보가 여기에 저장됩니다.
        private WorkOrderDto? _selectedWorkOrder;
        public WorkOrderDto? SelectedWorkOrder
        {
            get => _selectedWorkOrder;
            set { _selectedWorkOrder = value; OnPropertyChanged(nameof(SelectedWorkOrder)); }
        }

        // IsLoading: "지금 데이터 불러오는 중이에요"를 알려주는 스위치입니다. (로딩바 표시용)
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        // [추가] 화면에 표시할 통계용 프로퍼티
        private string _statisticsSummary = "통계 대기 중...";
        public string StatisticsSummary
        {
            get => _statisticsSummary;
            set { _statisticsSummary = value; OnPropertyChanged(nameof(StatisticsSummary)); }
        }

        //프로그래스바 퍼센트를 늘리기위한 변수
        private double? _LoadingProgress;
        public double? LoadingProgress
        {
            get => _LoadingProgress;
            set { _LoadingProgress = value; OnPropertyChanged(nameof(LoadingProgress)); }
        }
        //작업시작시 진행중일경우에는 값이 변경되지않도록
        private bool _flagYn;
        public bool FlagYn
        {
            get => _flagYn;
            set { _flagYn = value; OnPropertyChanged(nameof(FlagYn)); }
        }

        // 검색 기간 설정
        private DateTime _startDate = DateTime.Now.AddDays(-30); // 기본 일주일 전
        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
        }

        // 검색어 (지시 ID나 품목코드)
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); }
        }

        // [3. 버튼 명령] XAML의 버튼과 연결될 '리모컨 버튼'들입니다.
        public ICommand SearchCommand { get; }         // 검색명령
        public ICommand LoadCommand { get; }           // 새로고침
        public ICommand AddCommand { get; }            // 등록
        public ICommand EditCommand { get; }           // 수정
        public ICommand DeleteCommand { get; }         // 삭제
        public ICommand StartWorkCommand { get; }      // 작업시작
        public ICommand CompleteWorkCommand { get; }   // 작업완료

        // [명령 선언]
        public ICommand StartCommand { get; }// 작업시작
        public ICommand StopCommand { get; }// 작업완료

        public ICommand RegisterResultCommand { get; } // 실적등록
        public ICommand ViewResultsCommand { get; }    // 실적조회
        public ICommand CalculateStatsCommand { get; }  // [추가] 계산 명령

        // [4. 창 띄우기 이벤트] "창 좀 열어줘!"라고 화면(View)에 보내는 신호들입니다.
        public event Action<WorkResultRegisterViewModel, string>? OpenRegisterWindowRequested;
        public event Action<WorkOrderEditViewModel, string>? OpenEditWindowRequested;
        public event Action<WorkResultListViewModel, string>? OpenResultWindowRequested;

        // A. XAML 디자인 및 기본 생성을 위한 "빈 생성자" (추가)
        public WorkOrderListViewModel() : this(new WorkOrderService(), new WorkResultService(), new WorkOrderRepository(), new SerialDeviceService()) // 아래 B번 생성자를 호출하며 실제 객체를 전달
        {
            // 아무것도 적지 않아도 됩니다. (위의 : this(...)가 알아서 연결해줍니다.)
        }

        // [5. 생성자] 이 클래스가 태어날 때 가장 먼저 실행되는 곳입니다.
        // B. 실제 프로그램 실행 및 테스트를 위한 "기존 생성자" (유지)
        public WorkOrderListViewModel(IWorkOrderService workOrderService, IWorkResultService WorkResultsService, IWorkOrderRepository WorkOrderRepository, SerialDeviceService SerialDeviceService)
        {
            _WorkResultsService = WorkResultsService;
            _service = workOrderService;
            _workOrderRepository = WorkOrderRepository;
            _serialDeviceService = SerialDeviceService ?? throw new ArgumentNullException(nameof(SerialDeviceService));
            FlagYn = true; //초기값 true

            // [추가] PLC 서비스 이벤트 구독 (실시간 데이터 수신 시 UI 갱신)
            _serialDeviceService.OnDataReceived += (data) => {
                App.Current.Dispatcher.Invoke(() => {
                    AddLog($"RX: {data}"); // 로그 추가
                    PacketCount++;         // 패킷 수 증가
                    LastSignalTime = DateTime.Now.ToString("HH:mm:ss.fff");
                });
            };

            _serialDeviceService.OnDeviceStarted += (eqCode) => {
                App.Current.Dispatcher.Invoke(() => {
                    DeviceStatusText = $"{eqCode} 연결됨";
                    DeviceStatusColor = Brushes.LimeGreen; // 상태 표시등 녹색
                    AddLog($"SYSTEM: {eqCode} 설비와 통신이 시작되었습니다.");
                });
            };

            // 시리얼 서비스에서 데이터 수신 시 자동 새로고침 연결
            _serialDeviceService.OnRefreshRequired += (flag) => {
                 //FlagYn = flag;
                App.Current.Dispatcher.InvokeAsync(async () => await ExecuteLoadCommandAsync());
            };

            // [추가] 설비 가동 시작 신호 수신 시 로직
            _serialDeviceService.OnDeviceStarted += (eqCode) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    // 통계바나 상태 표시줄에 메시지 출력
                    StatisticsSummary = $"▶ 설비 상태: {eqCode} 가동 중 (신호 수신 완료)";

                    // 필요하다면 작은 팝업이나 알림바를 띄울 수 있습니다.
                     MessageBox.Show($"{eqCode} 설비가 정상적으로 가동되었습니다.");
                });
            };

            // [추가] 설비에서 "END" 전문이 오면 실행될 로직 연결
            _serialDeviceService.OnWorkFinishedByDevice += (eqCode) =>
            {
                // UI 스레드에서 안전하게 종료 메서드 호출
                App.Current.Dispatcher.Invoke(async () =>
                {
                    // 현재 선택된 작업이 해당 설비의 작업인지 확인 후 종료 처리
                    if (SelectedWorkOrder != null)
                    {
                        await ExecuteStopWork();
                        MessageBox.Show($"설비({eqCode})로부터 종료 신호를 수신하여 작업을 자동 마감합니다.");
                    }
                });
            };

            // [추가] 통신 재연결 체크
            _serialDeviceService.OnConnectionStatusChanged += (isConnected) =>
            {
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => {
                        // 1. 상태 텍스트 변경
                        DeviceStatusText = isConnected ? "🟢 설비 연결됨" : "🔴 연결 끊김 (재시도 중)";

                        // 2. 색상 변경
                        DeviceStatusColor = isConnected ? Brushes.LimeGreen : Brushes.Red;

                        // 3. 로그 남기기 (선택 사항)
                        AddLog(isConnected ? "SYSTEM: 설비와 연결되었습니다." : "SYSTEM: 설비 연결이 해제되었습니다.");
                    })
                );
            };

            //목록초기화
            StatusOptions = new ObservableCollection<StatusItem>
            {
                new StatusItem { DisplayName = "전체", StatusCode = "ALL" },
                new StatusItem { DisplayName = "대기", StatusCode = "W" },
                new StatusItem { DisplayName = "진행중", StatusCode = "P" },
                new StatusItem { DisplayName = "완료", StatusCode = "C" }
            };
            //필드에 직접 값을 넣어서 Setter에 의한 자동 호출을 일단 방지합니다. 기본값 all설정
            _selectedStatusCode = "ALL";
            OnPropertyChanged(nameof(SelectedStatusCode));// 화면에만 "전체"라고 표시

            // [보안] 컬렉션 동기화 활성화 (다른 스레드에서 WorkOrders를 건드려도 안전하게 함)
            BindingOperations.EnableCollectionSynchronization(WorkOrders, new object());


            // 리모컨 버튼(Command)과 실제 행동(Method)을 연결해줍니다.
            // async/await는 "작업이 끝날 때까지 기다려줄게, 하지만 화면은 멈추지 마"라는 뜻입니다.
            LoadCommand = new RelayCommand(async () => await ExecuteLoadCommandAsync());
            AddCommand = new RelayCommand(async () => await ExecuteAddCommandAsync(), CanExecuteAddCommand);
            EditCommand = new RelayCommand(async () => await ExecuteEditCommandAsync(), CanExecuteEditOrDeleteCommand);
            DeleteCommand = new RelayCommand(async () => await ExecuteDeleteCommandAsync(), CanExecuteEditOrDeleteCommand);

            StartWorkCommand = new RelayCommand(async () => await ExecuteStartWorkCommandAsync(), CanExecuteStartWorkCommand);
            CompleteWorkCommand = new RelayCommand(async () => await ExecuteCompleteWorkCommandAsync(), CanExecuteCompleteWorkCommand);

            RegisterResultCommand = new RelayCommand(async () => await ExecuteRegisterResultCommandAsync(), CanRegExecuteEditOrDeleteCommand);
            ViewResultsCommand = new RelayCommand(async () => await ExecuteViewResultsCommnad(), CanExcuteViewResultsCommand);
            // 생성자 안에서 연결
            CalculateStatsCommand = new RelayCommand(async () => await ExecuteCalculateStatsAsync());
            //작업시작, 작업종료 (plc연동 추가부분)
            StartCommand = new RelayCommand(async () => await ExecuteStartWork(), () => CanExecuteStart());
            StopCommand = new RelayCommand(async () => await ExecuteStopWork(), () => CanExecuteStop());

            // 검색 버튼과 연결될 명령 (ExecuteLoadCommandAsync를 재사용하되 검색 로직 포함)
            SearchCommand = new RelayCommand(async () => await ExecuteSearchAsync());

            // [추가] 디자인 모드(Visual Studio 디자이너 화면)라면 여기서 중단!
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;
            // 화면이 켜지자마자 데이터를 한 번 불러옵니다.
            //if (!isDesigner)
            //{
            _ = ExecuteLoadCommandAsync();//비동기 함수(async Task)를 호출할 때, 이 함수가 끝날 때까지 기다리지 않고
                                              //**"결과는 나중에 알아서 나오겠지, 일단 난 내 할 일 하러 갈게"**라고 선언하는 것입니다.
            //}
        }

        // [6. 조건 체크] 버튼을 누를 수 있는 상태인지 검사합니다. (true면 버튼 활성화, false면 비활성화)
        private bool CanExcuteViewResultsCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Complete; // 항목을 골라야 조회 가능
        private bool CanExecuteAddCommand() => true; // 등록은 언제나 가능
        private bool CanExecuteEditOrDeleteCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Wait; // '대기' 상태만 수정/삭제 가능
        private bool CanRegExecuteEditOrDeleteCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Processing; // '진행중'일 때만 실적 등록 가능
        private bool CanExecuteStartWorkCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Wait; // '대기'일 때만 시작 가능
        private bool CanExecuteCompleteWorkCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Processing; // '진행중'일 때만 완료 가능



        // [7. 실제 행동들] 버튼을 눌렀을 때 실행되는 비동기 로직입니다.
        // 데이터를 불러오는 로직
        // [개선] 스레드 안전성과 취소 로직이 추가된 로드 명령
        private async Task ExecuteLoadCommandAsync()
        {
            // [수정] 1. 이미 로딩 중이라면 중복 실행 방지 (가장 먼저 체크)
            if (_isLoading) return;

            // 2. 취소 토큰 설정
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;
            
            try
            {
                //if (FlagYn == true)
                //{
                    IsLoading = true;// 로딩 시작 (XAML의 오버레이가 나타남)
                    StatisticsSummary = "데이터를 불러오는 중...";

                    // DB에서 데이터를 가져오는 동안 UI가 멈추지 않도록 비동기 호출a
                    var data = await _service.GetAllWorkOrdersAsync(SelectedStatusCode
                        ,StartDate, EndDate, SearchText);

                    var dataList = data.ToList();
                    double totalCount = dataList.Count; // 전체 개수 파악

                    //작업이 취소되었는지 중간에 체크
                    if (token.IsCancellationRequested) return;

                    // ObservableCollection은 UI 스레드에서 갱신해야 하지만, 
                    // await 이후에는 자동으로 UI 스레드로 복귀하므로 바로 작성 가능합니다.
                    WorkOrders.Clear();
                    LoadingProgress = 0;//시작 전 0% 초기화

                    var statsTask = Task.Run(() =>
                    {
                        var total = dataList.Sum(x => x.Quantity);
                        var complete = dataList.Count(x => x.StatusEnum == WorkOrderStatus.Complete);
                        return $"총 지시수량: {total:N0} | 완료 건수: {complete:N0}건";
                    }, token);

                    // 3. UI 리스트 채우기 (Batch 처리)
                    /*
                     등급 (Priority)의미설명
                    Send (10)최우선당장 멈추고 이 일부터 해! (거의 안 씀)
                    Normal (9)보통일반적인 버튼 클릭 처리 등 (기본값)
                    Render (7)화면 그리기ProgressBar 애니메이션, 레이아웃 재계산 등
                    Background (4)백그라운드지금 우리가 쓴 것. 화면 다 그리고 남는 시간에 해!
                    ApplicationIdle (2)한가할 때프로그램이 정말 아무것도 안 하고 놀 때 해!
                     */
                    int batchSize = 2000;
                    for (int i = 0; i < dataList.Count; i += batchSize)//1 씩증가가 아닌 한번루프 돌때마다 1000개씩 증가
                    {
                        // 중요: 루프 마다 취소되었는지 확인!
                        if (token.IsCancellationRequested) return;

                        var batch = dataList.Skip(i).Take(batchSize).ToList();//i번째 만큼 건너뛰고 1000개씩 가져온다. 

                        await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            foreach (var item in batch) WorkOrders.Add(item);
                            // 퍼센트 계산: (현재까지 쌓인 양 / 전체 양) * 100
                            if (totalCount > 0)
                            {
                                LoadingProgress = ((double)(i + batch.Count) / totalCount) * 100;
                            }

                        }), System.Windows.Threading.DispatcherPriority.Background);

                        StatisticsSummary = $"{i + batch.Count:N0}건 로드 중...";
                        await Task.Delay(1); // UI 스레드에게 숨 쉴 틈 제공
                    }

                    // 4. 리스트 추가가 끝나면 미리 돌려놨던 통계 결과를 가져와 표시
                    StatisticsSummary = await statsTask;
                    LoadingProgress = 100;//완료시 100
                    // 2. [병렬 처리] 데이터 추가와 통계 연산을 동시에 시작합니다.
                    // 통계 연산 작업 시작 (결과는 나중에 await로 받음)
                    //Task.Run을 사용하는 핵심적인 이유는 딱 한 문장으로 요약됩니다:
                    //**"무거운 짐을 UI 스레드에서 떼어내어, 힘센 일꾼(백그라운드 스레드)에게 맡기기 위해서"**입니다.
                    /*
                     1. CPU 집약적인 작업 (CPU-Bound Tasks)
                    컴퓨터의 머리(CPU)가 아주 바쁘게 연산해야 하는 경우입니다. 현재 하신 **'10만 건 데이터 합계 계산'**이 여기에 해당합니다.
                    복잡한 수학 계산: 통계 분석, 암호화/복복호화.
                    이미지/비디오 처리: 사진 필터 적용, 이미지 리사이징.
                    대량의 텍스트 분석: 수만 줄의 로그 파일에서 특정 패턴 찾기.
                    이유: 이런 일을 UI 스레드가 직접 하면, 계산하는 동안 마우스 클릭도 안 먹히고 화면이 멈추기 때문입니다.

                    2. 동기(Sync) 메서드를 비동기처럼 쓰고 싶을 때
                    우리가 사용하는 외부 라이브러리 중에는 async가 지원되지 않는 옛날 방식들이 있습니다.
                    예시: Library.DoHeavyJob() 처럼 시간이 오래 걸리는데 Task를 반환하지 않는 경우.
                    C#
                    // 이렇게 감싸면 동기 메서드도 비동기처럼 호출 가능합니다.
                    await Task.Run(() => _oldService.OldHeavyMethod());
                    이유: 내가 직접 코드를 고칠 수 없는 외부 도구를 쓰면서도 UI가 멈추는 것을 방지하기 위해서입니다.

                    3. I/O 작업이지만 응답이 너무 느릴 때
                    일반적으로 DB 조회나 웹 API 호출은 await _service.GetAsync()처럼 전용 비동기 메서드를 쓰는 게 정석입니다. 하지만 다음과 같은 특수한 경우가 있습니다.
                    로컬 파일 대량 읽기/쓰기: 하드디스크 속도가 느려 윈도우 메세지 처리에 영향을 줄 때.
                    복합 작업: 데이터를 가져오자마자(I/O) 바로 복잡하게 가공(CPU)해야 하는 세트 작업.

                    4. 병렬 처리 (Parallelism)
                    여러 개의 일을 동시에 처리해서 전체 시간을 단축하고 싶을 때 사용합니다.
                    예시: 1번 공정 데이터, 2번 공정 데이터, 3번 공정 데이터를 각각 다른 스레드에서 동시에 불러와서 합칠 때.

                    1. 여러 공정 데이터 동시 조회 (속도 향상)
                    만약 '작업 지시', '재고 현황', '불량 내역' 3가지 데이터를 각각 불러와야 한다면, 하나씩 기다리는 것(await)보다 동시에 던지는(Task.Run) 것이 훨씬 빠릅니다.
                    일반 방식: 1초 + 1초 + 1초 = 3초 소요
                    병렬 방식: 1초, 1초, 1초 동시 시작 = 최대 1초 소요
                    C#
                    public async Task LoadDashboardDataAsync()
                    {
                        IsLoading = true;

                        // 3개의 작업을 동시에 시작합니다.
                        var orderTask = _orderService.GetAllOrdersAsync();      // 작업지시 조회
                        var stockTask = _stockService.GetCurrentStockAsync();   // 재고 조회
                        var defectTask = _defectService.GetDefectLogsAsync();   // 불량 로그 조회

                        // Task.WhenAll을 사용하여 세 작업이 모두 끝날 때까지 기다립니다.
                        await Task.WhenAll(orderTask, stockTask, defectTask);

                        // 결과물들을 한 번에 UI에 반영
                        Orders = new ObservableCollection<OrderDto>(await orderTask);
                        Stocks = new ObservableCollection<StockDto>(await stockTask);
                        Defects = new ObservableCollection<DefectDto>(await defectTask);

                        IsLoading = false;
                    }
                    2. 대량 데이터 병렬 계산 (Parallel.ForEach)
                    10만 건의 작업 지시 데이터를 분석하여, 각 항목마다 복잡한 '예상 완료 시간'을 계산해야 한다고 가정해 봅시다. 
                    foreach 문을 하나씩 돌리는 대신, CPU의 여러 코어를 동시에 사용하여 계산합니다.

                    C#
                    public async Task CalculateEstimatedTimesAsync()
                    {
                        var data = WorkOrders.ToList();

                        await Task.Run(() =>
                        {
                            // Parallel.ForEach가 CPU 코어를 나눠서 병렬로 계산합니다.
                            Parallel.ForEach(data, item =>
                            {
                                // 아주 복잡하고 무거운 연산 (예: 설비 가동률 기반 시뮬레이션)
                                item.EstimatedTime = ComplexCalculation(item);
                            });
                        });

                        MessageBox.Show("모든 데이터의 예상 시간이 계산되었습니다.");
                    }
                    3. 실시간 설비 모니터링 (개별 백그라운드 루프)
                    MES에서 여러 대의 설비(PLC)로부터 데이터를 받아올 때, 메인 UI 스레드에서 하나씩 체크하면 화면이 버벅입니다. 각 설비마다 별도의 Task를 할당하여 병렬로 감시합니다.

                    C#
                    public void StartMonitoring()
                    {
                        // 설비 A와 설비 B를 각각 별도의 일꾼(Task)이 감시하게 함
                        Task.Run(() => MonitorMachine("Machine_A"));
                        Task.Run(() => MonitorMachine("Machine_B"));
                    }

                    private async Task MonitorMachine(string machineId)
                    {
                        while (true)
                        {
                            var status = await _plcService.ReadStatusAsync(machineId);

                            // UI 스레드에 상태 보고
                            App.Current.Dispatcher.Invoke(() => {
                                UpdateMachineUI(machineId, status);
                            });

                            await Task.Delay(500); // 0.5초마다 체크
                        }
                    }
                     */
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;// 반드시 false로 돌려주어 로딩바를 제거
            }
        }

        private async Task ExecuteAddCommandAsync()
        {
            var newDto = new WorkOrderDto { Id = 0, ItemCode = "", Quantity = 0, Status = "w" };
            var editVm = new WorkOrderEditViewModel(newDto);

            OpenEditWindowRequested?.Invoke(editVm, "작업지시등록");

            if (editVm.IsSaved)
            {
                var savedDto = editVm.CommitChanges();
                // 비동기 서비스 메서드가 없다면 Task.Run으로 감싸 UI 멈춤 방지
                await _service.CreateWorkOrder(savedDto);

                await ExecuteLoadCommandAsync();
                MessageBox.Show("정상적으로 등록 되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        //지시수정
        private async Task ExecuteEditCommandAsync()
        {
            if (SelectedWorkOrder == null) return;
            var editVm = new WorkOrderEditViewModel(SelectedWorkOrder);

            OpenEditWindowRequested?.Invoke(editVm, "작업 지시 수정");

            if (editVm.IsSaved)
            {
                var updatedDto = editVm.CommitChanges();
                // DB 반영
                await _service.UpdateWorkOrder(updatedDto);
                // 목록 갱신
                await ExecuteLoadCommandAsync();

                MessageBox.Show("수정 되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 삭제 로직 (MessageBox로 한 번 더 물어보기 추가)
        private async Task ExecuteDeleteCommandAsync()
        {
            if (SelectedWorkOrder == null) return;

            MessageBoxResult result = MessageBox.Show("정말로 삭제하시겠습니까?", "삭제확인",
              MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _service.DeleteWorkOrder(SelectedWorkOrder.Id);
                await ExecuteLoadCommandAsync();
                MessageBox.Show("정상적으로 삭제 되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        //작업지시 등록
        // [참고] 버튼 클릭 이벤트에서 async Task를 사용할 때의 주의사항
        private async Task ExecuteStartWorkCommandAsync()
        {
            if (SelectedWorkOrder == null) return;

            try
            {
                // 1. 상태 업데이트 시도
                await _service.UpdateWorkOrderStatus(SelectedWorkOrder.Id, WorkOrderStatus.Processing);

                // 2. 성공했으니 리스트 새로고침
                await ExecuteLoadCommandAsync();

                MessageBox.Show("작업이 시작되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // DB 오류 등으로 실패했을 경우
                MessageBox.Show($"작업 시작 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteCompleteWorkCommandAsync()
        {
            if (SelectedWorkOrder == null) return;
            // 상태 업데이트 비동기 처리
            //await Task.Run(() => _service.UpdateWorkOrderStatus(SelectedWorkOrder.Id, WorkOrderStatus.Complete));
            await _service.UpdateWorkOrderStatus(SelectedWorkOrder.Id, WorkOrderStatus.Complete);
            await ExecuteLoadCommandAsync();
        }

        private async Task ExecuteRegisterResultCommandAsync()
        {
            if (SelectedWorkOrder == null) return;
            var registerVm = new WorkResultRegisterViewModel(SelectedWorkOrder);

            OpenRegisterWindowRequested?.Invoke(registerVm, $"실적 등록: {SelectedWorkOrder.ItemCode}");
            // 2. 창이 닫힌 후, 코드는 바로 다음 줄로 넘어옵니다.
            // 3. 이때 'DialogResult = true'가 실행되면서 동시에 ViewModel의 'IsSaved'도 true가 되었을 겁니다.
            if (registerVm.IsSaved)// <--- 여기서 DialogResult 대신 IsSaved 변수로 성공 여부를 체크 중!
            {
                await ExecuteLoadCommandAsync();
                MessageBox.Show("실적이 성공적으로 등록되었으며, 작업 지시가 완료 처리되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task ExecuteViewResultsCommnad()
        {
            if (SelectedWorkOrder == null) return;
            // 주입받은 필드(_workResultService)를 그대로 자식 ViewModel에 넘겨줍니다.
            var listViewModel = new WorkResultListViewModel(SelectedWorkOrder, _WorkResultsService);

            await listViewModel.ExecuteLoadResultsCommand();
            OpenResultWindowRequested?.Invoke(listViewModel, $"작업 실적 조회: {SelectedWorkOrder.ItemCode}");
        }

        // ---------------------------------------------------------------------
        // 신규 로직: 작업 시작 실행
        // ---------------------------------------------------------------------
        private async Task ExecuteStartWork()
        {
            if (SelectedWorkOrder == null || IsLoading) return; // 로딩 중이면 차단

            try
            {
                IsLoading = true; // 로딩 시작
                                  // 1. DB 상태 변경 (대기 -> 진행)
                await _workOrderRepository.StartWorkOrder(SelectedWorkOrder.Id, UserSession.UserId, "EQ01");

                // 2. PLC 통신 시작 (이때 포트를 엽니다)
                // _serialService는 주입받은 객체
                // [보완] 포트 이름을 설정에서 가져오거나 체크
                _serialDeviceService.Open("COM1");

                // 3. (옵션) PLC에게 시작 신호를 보내야 한다면
                //_serialDeviceService.SendMessage("START_ORDER");

                // --- 수정 포인트: 재조회 전 로딩 플래그 해제 ---
                // ExecuteLoadCommandAsync 내부에도 IsLoading을 true로 만드는 로직이 있으므로,
                // 여기서 미리 풀어주어야 return되지 않고 정상 실행됩니다.
                IsLoading = false;
                // 3. 목록 새로고침 (여기서 다시 IsLoading이 true가 되었다가 작업 후 false가 됨)
                await ExecuteLoadCommandAsync();

                // [추가] 버튼 상태 즉시 반영
                CommandManager.InvalidateRequerySuggested();

                // [추가] 실제 Serial 포트 오픈 및 모니터링 시작
                //_serialDeviceService.Open("COM1");
                AddLog("SYSTEM: COM1 포트 Open 시도...");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"설비 연결 실패: {ex.Message}");
            }
            finally
            {
                IsLoading = false; // 로딩 종료
            }
        }

        // ---------------------------------------------------------------------
        // 신규 로직: 작업 종료 실행
        // ---------------------------------------------------------------------
        private async Task ExecuteStopWork()
        {
            // 1. 사전 체크: 선택된 지시가 없거나 이미 로딩 중이면 종료
            if (SelectedWorkOrder == null || IsLoading) return;

            try
            {
                IsLoading = true; // [추가] 작업 시작 즉시 로딩 표시 및 버튼 잠금

                // 2. DB 마감 처리 (상태를 'C' 또는 종료로 변경)
                await _workOrderRepository.StopWorkOrder(SelectedWorkOrder.Id, UserSession.UserId);

                // 3. PLC 통신 종료
                // 주의: Dispose 후 다시 시작할 때 객체가 null이 되지 않도록 관리 필요
                _serialDeviceService.Dispose();

                DeviceStatusText = "통신 종료";
                DeviceStatusColor = Brushes.Gray;
                AddLog("SYSTEM: 통신 포트를 닫았습니다.");

                IsLoading = false;

                // 4. 목록 새로고침 (변경된 상태 반영)
                await ExecuteLoadCommandAsync();

                // [추가] 버튼 상태 즉시 반영
                CommandManager.InvalidateRequerySuggested();

                MessageBox.Show("작업이 정상적으로 종료되고 통신이 해제되었습니다.", "종료 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설비 연결 종료 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false; // 작업 완료 후 잠금 해제
            }
        }

        // 버튼 활성화 조건 (대기 중일 때만 시작 가능, 진행 중일 때만 종료 가능)
        private bool CanExecuteStart() => SelectedWorkOrder?.Status == "W";
        private bool CanExecuteStop() => SelectedWorkOrder?.Status == "P";

        private async Task ExecuteCalculateStatsAsync()
        {
            if (WorkOrders.Count == 0) return;

            IsLoading = true;
            StatisticsSummary = "계산 중...";

            try
            {
                // 핵심: 10만 건의 데이터를 분석하는 무거운 작업은 Task.Run으로 백그라운드 팀에 맡깁니다.
                var result = await Task.Run(() =>
                {
                    // 이 안은 UI 스레드가 아니므로 마음껏 CPU를 써도 화면이 멈추지 않습니다.
                    var totalQty = WorkOrders.Sum(x => x.Quantity);
                    var waitCount = WorkOrders.Count(x => x.StatusEnum == WorkOrderStatus.Wait);
                    var processingCount = WorkOrders.Count(x => x.StatusEnum == WorkOrderStatus.Processing);
                    var completeCount = WorkOrders.Count(x => x.StatusEnum == WorkOrderStatus.Complete);

                    return $"총 수량: {totalQty:N0} | 대기: {waitCount}건, 진행: {processingCount}건, 완료: {completeCount}건";
                });

                // await 이후에는 다시 UI 스레드로 돌아오므로 안전하게 프로퍼티를 갱신합니다.
                StatisticsSummary = result;
            }
            catch (Exception ex)
            {
                StatisticsSummary = "계산 오류";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // [추가] 로그 메시지 관리 메서드 (최신 로그 100개 유지)
        private void AddLog(string msg)
        {
            CommunicationLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            if (CommunicationLogs.Count > 100) CommunicationLogs.RemoveAt(100);
        }

        // 신규 검색 전용 메서드
        private async Task ExecuteSearchAsync()
        {
            // 검색 시에는 무조건 로딩 바를 보여주고 강제 재조회
            FlagYn = true;
            await ExecuteLoadCommandAsync();
        }
    }
}
/*
 

 */