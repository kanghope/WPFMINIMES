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
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;         // 비동기(딴짓하면서 일하기)를 위한 도구
using System.Windows;
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
        private WorkOrderDto _selectedWorkOrder;
        public WorkOrderDto SelectedWorkOrder
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

        // [3. 버튼 명령] XAML의 버튼과 연결될 '리모컨 버튼'들입니다.
        public ICommand LoadCommand { get; }           // 새로고침
        public ICommand AddCommand { get; }            // 등록
        public ICommand EditCommand { get; }           // 수정
        public ICommand DeleteCommand { get; }         // 삭제
        public ICommand StartWorkCommand { get; }      // 작업시작
        public ICommand CompleteWorkCommand { get; }   // 작업완료
        public ICommand RegisterResultCommand { get; } // 실적등록
        public ICommand ViewResultsCommand { get; }    // 실적조회

        // [4. 창 띄우기 이벤트] "창 좀 열어줘!"라고 화면(View)에 보내는 신호들입니다.
        public event Action<WorkResultRegisterViewModel, string> OpenRegisterWindowRequested;
        public event Action<WorkOrderEditViewModel, string> OpenEditWindowRequested;
        public event Action<WorkResultListViewModel, string> OpenResultWindowRequested;

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
            // 리모컨 버튼(Command)과 실제 행동(Method)을 연결해줍니다.
            // async/await는 "작업이 끝날 때까지 기다려줄게, 하지만 화면은 멈추지 마"라는 뜻입니다.
            LoadCommand = new RelayCommand(async () => await ExecuteLoadCommandAsync());
            AddCommand = new RelayCommand(async () => await ExecuteAddCommandAsync(), CanExecuteAddCommand);
            EditCommand = new RelayCommand(async () => await ExecuteEditCommandAsync(), CanExecuteEditOrDeleteCommand);
            DeleteCommand = new RelayCommand(async () => await ExecuteDeleteCommandAsync(), CanExecuteEditOrDeleteCommand);

            StartWorkCommand = new RelayCommand(async () => await ExecuteStartWorkCommandAsync(), CanExecuteStartWorkCommand);
            CompleteWorkCommand = new RelayCommand(async () => await ExecuteCompleteWorkCommandAsync(), CanExecuteCompleteWorkCommand);

            RegisterResultCommand = new RelayCommand(async () => await ExecuteRegisterResultCommandAsync(), CanRegExecuteEditOrDeleteCommand);
            ViewResultsCommand = new RelayCommand(ExecuteViewResultsCommnad, CanExcuteViewResultsCommand);

            // 화면이 켜지자마자 데이터를 한 번 불러옵니다.
            _ = ExecuteLoadCommandAsync();
        }

        // [6. 조건 체크] 버튼을 누를 수 있는 상태인지 검사합니다. (true면 버튼 활성화, false면 비활성화)
        private bool CanExcuteViewResultsCommand() => SelectedWorkOrder != null; // 항목을 골라야 조회 가능
        private bool CanExecuteAddCommand() => true; // 등록은 언제나 가능
        private bool CanExecuteEditOrDeleteCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Wait; // '대기' 상태만 수정/삭제 가능
        private bool CanRegExecuteEditOrDeleteCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Processing; // '진행중'일 때만 실적 등록 가능
        private bool CanExecuteStartWorkCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Wait; // '대기'일 때만 시작 가능
        private bool CanExecuteCompleteWorkCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Processing; // '진행중'일 때만 완료 가능

        // [7. 실제 행동들] 버튼을 눌렀을 때 실행되는 비동기 로직입니다.
        // 데이터를 불러오는 로직
        private async Task ExecuteLoadCommandAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                // DB에서 데이터를 가져오는 동안 UI가 멈추지 않도록 비동기 호출
                var data = await _service.GetAllWorkOrdersAsync();

                // ObservableCollection은 UI 스레드에서 갱신해야 하지만, 
                // await 이후에는 자동으로 UI 스레드로 복귀하므로 바로 작성 가능합니다.
                WorkOrders.Clear();
                foreach (var item in data)
                {
                    WorkOrders.Add(item);
                }
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

        private async Task ExecuteStartWorkCommandAsync()
        {
            if (SelectedWorkOrder == null) return;
            // 상태 업데이트 비동기 처리
            await _service.UpdateWorkOrderStatus(SelectedWorkOrder.Id, WorkOrderStatus.Processing);
            await ExecuteLoadCommandAsync();
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

        private void ExecuteViewResultsCommnad()
        {
            if (SelectedWorkOrder == null) return;
            // 주입받은 필드(_workResultService)를 그대로 자식 ViewModel에 넘겨줍니다.
            var listViewModel = new WorkResultListViewModel(SelectedWorkOrder, _WorkResultsService);
            OpenResultWindowRequested?.Invoke(listViewModel, $"작업 실적 조회: {SelectedWorkOrder.ItemCode}");
        }

        // 8. Property Changed Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}