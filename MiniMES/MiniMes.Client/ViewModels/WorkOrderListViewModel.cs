using MiniMes.Client;
using MiniMes.Client.Helpers;
using MiniMes.Client.ViewModels;
using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
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


namespace MiniMes.Client.ViewModels
{
    // INotifyPropertyChanged: "내 데이터가 바뀌면 화면에 알려주겠다"는 약속입니다.
    public class WorkOrderListViewModel : INotifyPropertyChanged
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

        // [3. 버튼 명령] XAML의 버튼과 연결될 '리모컨 버튼'들입니다.
        public ICommand LoadCommand { get; }           // 새로고침
        public ICommand AddCommand { get; }            // 등록
        public ICommand EditCommand { get; }           // 수정
        public ICommand DeleteCommand { get; }         // 삭제
        public ICommand StartWorkCommand { get; }      // 작업시작
        public ICommand CompleteWorkCommand { get; }   // 작업완료
        public ICommand RegisterResultCommand { get; } // 실적등록
        public ICommand ViewResultsCommand { get; }    // 실적조회
        public ICommand CalculateStatsCommand { get; }  // [추가] 계산 명령

        // [4. 창 띄우기 이벤트] "창 좀 열어줘!"라고 화면(View)에 보내는 신호들입니다.
        public event Action<WorkResultRegisterViewModel, string>? OpenRegisterWindowRequested;
        public event Action<WorkOrderEditViewModel, string>? OpenEditWindowRequested;
        public event Action<WorkResultListViewModel, string>? OpenResultWindowRequested;

        // A. XAML 디자인 및 기본 생성을 위한 "빈 생성자" (추가)
        public WorkOrderListViewModel() : this(new WorkOrderService(), new WorkResultService()) // 아래 B번 생성자를 호출하며 실제 객체를 전달
        {
            // 아무것도 적지 않아도 됩니다. (위의 : this(...)가 알아서 연결해줍니다.)
        }

        // [5. 생성자] 이 클래스가 태어날 때 가장 먼저 실행되는 곳입니다.
        // B. 실제 프로그램 실행 및 테스트를 위한 "기존 생성자" (유지)
        public WorkOrderListViewModel(IWorkOrderService workOrderService, IWorkResultService WorkResultsService)
        {
            _WorkResultsService = WorkResultsService;
            _service = workOrderService;

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
            // 1. 이전 작업이 수행 중이면 취소 신호를 보냅니다.
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            if (IsLoading) return;// 이미 로딩 중이면 중복 방지 (안전장치)
            
            IsLoading = true;// 로딩 시작 (XAML의 오버레이가 나타남)
            StatisticsSummary = "데이터를 불러오는 중...";
            try
            {
                // DB에서 데이터를 가져오는 동안 UI가 멈추지 않도록 비동기 호출
                var data = await _service.GetAllWorkOrdersAsync();

                //작업이 취소되었는지 중간에 체크
                if (token.IsCancellationRequested) return;

                // ObservableCollection은 UI 스레드에서 갱신해야 하지만, 
                // await 이후에는 자동으로 UI 스레드로 복귀하므로 바로 작성 가능합니다.
                WorkOrders.Clear();

                /*
                foreach (var item in data)
                {
                    // 수만 건일 경우를 대비해 취소 체크를 루프 안에서도 수행 가능
                    if (token.IsCancellationRequested) break;
                    WorkOrders.Add(item);
                }
                // 3. [Task.Run 활용] CPU 작업: 10만 건 통계 계산을 백그라운드에서 실행
                // 이 과정 동안 ProgressBar 애니메이션은 멈추지 않고 부드럽게 돌아갑니다.
                StatisticsSummary = "분석 연산 중...";
                
                string statsResult = await Task.Run(() =>
                {
                    //실제 무거운 연산(10만건 루프)
                    var total = data.Sum(x => x.Quantity);
                    var complete = data.Count(x => x.StatusEnum == WorkOrderStatus.Complete);
                    //의도적인 부하 테스트를 위해 복잡한 가공이 들어가는 곳입니다.
                    return $"총 지시수량: {total:N0} | 완료 건수:{complete:N0}건";
                }, token);
                StatisticsSummary = statsResult; // 계산 완료 후 UI 반영
                */

    
                var dataList = data.ToList();

                // 2. [병렬 처리] 데이터 추가와 통계 연산을 동시에 시작합니다.
                // 통계 연산 작업 시작 (결과는 나중에 await로 받음)
                var statsTask = Task.Run(() =>
                {
                    var total = dataList.Sum(x => x.Quantity);
                    var complete = dataList.Count(x => x.StatusEnum == WorkOrderStatus.Complete);
                    return $"총 지시수량: {total:N0} | 완료 건수: {complete:N0}건";
                }, token);

                // 3. UI 리스트 채우기 (Batch 처리)
                int batchSize = 1000;
                for (int i = 0; i < dataList.Count; i += batchSize)
                {
                    // 중요: 루프 마다 취소되었는지 확인!
                    if (token.IsCancellationRequested) return;

                    var batch = dataList.Skip(i).Take(batchSize).ToList();

                    await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (var item in batch) WorkOrders.Add(item);
                    }), System.Windows.Threading.DispatcherPriority.Background);

                    StatisticsSummary = $"{i + batch.Count:N0}건 로드 중...";
                    await Task.Delay(1); // UI 스레드에게 숨 쉴 틈 제공
                }

                // 4. 리스트 추가가 끝나면 미리 돌려놨던 통계 결과를 가져와 표시
                StatisticsSummary = await statsTask;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
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

        // 8. Property Changed Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
    }
}