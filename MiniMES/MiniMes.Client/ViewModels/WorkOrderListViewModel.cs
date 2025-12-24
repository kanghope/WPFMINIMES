using MiniMes.Client;
using MiniMes.Client.ViewModels;
using System;
using System.Collections.ObjectModel; // 컬렉션 변경 시 UI 자동 갱신을 위한 ObservableCollection 사용
using System.ComponentModel;      // 속성 변경 알림(INotifyPropertyChanged)을 위해 필요
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // MessageBox를 사용하기 위해 추가
using System.Windows.Input;       // 명령(ICommand) 인터페이스 사용
using MiniMes.Client.Helpers;     // RelayCommand 헬퍼 클래스 참조
using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;       // UI에 보여줄 데이터 형태(DTO) 참조
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services; // DB 접근 로직(Service) 참조



namespace MiniMes.Client.ViewModels

{

    // INotifyPropertyChanged 구현: 이 ViewModel의 속성 값이 바뀌면 View(XAML)에 알립니다.

    public class WorkOrderListViewModel : INotifyPropertyChanged

    {

        // ---------------------------------------------------------------------

        // 1. Dependency: Service 인스턴스 (DB 접근 로직을 담당)

        // ---------------------------------------------------------------------



        // Service 인스턴스를 생성하여 DB 관련 작업은 이 객체를 통해 수행합니다.

        private readonly WorkOrderService _service = new WorkOrderService();





        // ---------------------------------------------------------------------

        // 2. Data Properties (데이터 속성)

        // ---------------------------------------------------------------------



        // _workOrders는 실제 데이터 컬렉션을 저장하는 private 필드입니다.

        private ObservableCollection<WorkOrderDto> _workOrders = new ObservableCollection<WorkOrderDto>();



        // WorkOrders: DataGrid의 ItemsSource(데이터 원본)와 바인딩될 Public 속성입니다.

        // ObservableCollection을 사용하면 항목이 추가/삭제될 때 XAML DataGrid가 자동으로 업데이트됩니다.

        public ObservableCollection<WorkOrderDto> WorkOrders

        {

            get => _workOrders;

            set { _workOrders = value; OnPropertyChanged(nameof(WorkOrders)); } // Setter에서 UI에 변경을 알립니다.

        }



        //private ObservableCollection<WorkResultDto> _workResults = new ObservableCollection<WorkResultDto>();



        //public ObservableCollection<WorkResultDto> WorkResults

        //{

        //    get { return _workResults;  }

        //    set { _workResults = value; OnPropertyChanged(nameof(WorkResults)); }

        //}



        // _selectedWorkOrder는 DataGrid에서 사용자가 선택한 행의 데이터를 저장하는 필드입니다.

        private WorkOrderDto _selectedWorkOrder;



        // SelectedWorkOrder: DataGrid의 SelectedItem과 바인딩될 Public 속성입니다.

        public WorkOrderDto SelectedWorkOrder // DataGrid의 SelectedItem과 바인딩

        {

            get => _selectedWorkOrder;

            // Setter가 호출될 때마다 OnPropertyChanged를 통해 View의 다른 요소(예: 상세 정보 창)에 변경을 알립니다.

            set { _selectedWorkOrder = value; OnPropertyChanged(nameof(SelectedWorkOrder)); }

        }



        // Service 인스턴스를 생성하여 DB 관련 작업은 이 객체를 통해 수행합니다.

        private readonly WorkResultService _WorkResultsService = new WorkResultService();



        // ---------------------------------------------------------------------

        // 3. Command Properties (명령 속성)

        // ---------------------------------------------------------------------



        // LoadCommand: '새로 고침' 버튼과 바인딩되어 데이터 로드 로직을 실행합니다.

        public ICommand LoadCommand { get; }



        // AddCommand: '지시 등록' 버튼과 바인딩되어 새 지시 등록 로직을 실행합니다.

        public ICommand AddCommand { get; }



        public ICommand EditCommand { get; }

        public ICommand DeleteCommand { get; }



        // --- 상태 변경 Command 추가 ---

        public ICommand StartWorkCommand { get; }

        public ICommand CompleteWorkCommand { get; }





        // [추가] 실적 등록 명령

        public ICommand RegisterResultCommand { get; }



        // [추가] 실적 등록 팝업 제어를 위한 이벤트

        public event Action<WorkResultRegisterViewModel, string> OpenRegisterWindowRequested;



        // **팝업 창 제어를 위한 이벤트** (ViewModel -> View 통신 채널)

        public event Action<WorkOrderEditViewModel, string> OpenEditWindowRequested;



        // [추가] 실적 조회 명령 속성

        public ICommand ViewResultsCommand { get; }



        // [추가] 실적 조회 팝업 제어를 위한 이벤트

        public event Action<WorkResultListViewModel, string> OpenResultWindowRequested;



        // ---------------------------------------------------------------------

        // 4. Constructor (생성자)

        // ---------------------------------------------------------------------



        public WorkOrderListViewModel()

        {

            // Command 초기화: RelayCommand를 사용하여 UI 이벤트와 C# 메서드를 연결합니다.

            // LoadCommand: 클릭 시 ExecuteLoadCommandAsync() 실행

            LoadCommand = new RelayCommand(async () => await ExecuteLoadCommandAsync());



            // AddCommand: 클릭 시 ExecuteAddCommand() 실행, CanExecuteAddCommand()로 활성화 여부 판단

            AddCommand = new RelayCommand(ExecuteAddCommand, CanExecuteAddCommand);



            EditCommand = new RelayCommand(ExecuteEditCommand, CanExecuteEditOrDeleteCommand);

            DeleteCommand = new RelayCommand(ExecuteDeleteCommand, CanExecuteEditOrDeleteCommand);







            // --- 상태 변경 Command 초기화 ---

            // 선택된 항목이 있고 현재 상태가 '대기'일 때만 시작 가능하도록 설정

            StartWorkCommand = new RelayCommand(ExecuteStartWorkCommand, CanExecuteStartWorkCommand);

            // 선택된 항목이 있고 현재 상태가 '진행 중'일 때만 완료 가능하도록 설정

            CompleteWorkCommand = new RelayCommand(ExecuteCompleteWorkCommand, CanExecuteCompleteWorkCommand);



            // [추가] RegisterResultCommand 초기화

            RegisterResultCommand = new RelayCommand(ExecuteRegisterResultCommand, CanRegExecuteEditOrDeleteCommand);

            // --------------------------------



            // [추가] ViewResultsCommand 초기화 (선택된 항목이 있으면 항상 조회 가능)

            ViewResultsCommand = new RelayCommand(ExecuteViewResultsCommnad, CanExcuteViewResultsCommand);



            // ViewModel 생성 시 (화면이 뜰 때) 데이터를 한 번 로드하여 초기 화면을 채웁니다.

            ExecuteLoadCommandAsync();

        }



        // [추가] CanExecute 로직 (선택된 항목이 null이 아닐 때만 조회 가능)

        private bool CanExcuteViewResultsCommand() => SelectedWorkOrder != null;



        // [추가] Execute 로직

        private void ExecuteViewResultsCommnad()

        {

            if (SelectedWorkOrder == null) return;

            // 1. 조회 팝업 ViewModel 생성 및 선택된 DTO 전달

            // WorkResultListViewModel은 IWorkResultService가 필요합니다.

            var listViewModel = new WorkResultListViewModel(SelectedWorkOrder, _WorkResultsService);



            //2. View에 팝업 창을 열어달라고 요청

            OpenResultWindowRequested?.Invoke(listViewModel, $"작업 실적 조회: {SelectedWorkOrder.ItemCode}");

        }



        // 선택된 항목이 있을 때만 수정/삭제 가능하도록 로직 정의

        private bool CanExecuteEditOrDeleteCommand() => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Wait;



        private bool CanRegExecuteEditOrDeleteCommand()

        {

            return SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Processing;

        }

        // ---------------------------------------------------------------------

        // 5. Command Execution Methods (실행 로직)

        // ---------------------------------------------------------------------



        private async Task ExecuteLoadCommandAsync()

        {

            var IsLoading = true; // 로딩 상태 UI에 표시 (바인딩 필요)

            try

            {

                WorkOrders.Clear(); // 기존 목록을 비웁니다.



                // Service를 호출하여 DB에서 DTO 목록을 가져옵니다. (Infrastructure 계층의 책임)

                var data = await _service.GetAllWorkOrdersAsync();



                // 가져온 DTO 목록을 View에 바인딩된 ObservableCollection에 추가합니다.

                // IEnumerable<T>의 ForEach 대신 Linq의 ToList().ForEach를 사용하여 컬렉션에 추가

                // UI 스레드에서 ObservableCollection 업데이트

                App.Current.Dispatcher.Invoke(() =>

                {

                    data.ToList().ForEach(WorkOrders.Add);

                });

            }

            catch (Exception ex)

            {

                // 오류 처리

                Console.WriteLine(ex.Message);

            }

            finally

            {

                IsLoading = false;

            }

        }

        private void ExecuteRegisterResultCommand()

        {

            if (SelectedWorkOrder == null) return;



            // 1. 등록 팝업 ViewModel 생성 및 선택된 DTO 전달

            var registerVm = new WorkResultRegisterViewModel(SelectedWorkOrder);



            // 2. View에 팝업 창을 열어달라고 요청

            OpenRegisterWindowRequested?.Invoke(registerVm, $"실적 등록: {SelectedWorkOrder.ItemCode}");



            // 3. 팝업이 닫힌 후, 저장이 성공했는지 확인

            if (registerVm.IsSaved)

            {

                // DB 저장 후 WorkOrder 상태가 완료로 변경되었을 것이므로 목록을 갱신

                ExecuteLoadCommandAsync();

                MessageBox.Show("실적이 성공적으로 등록되었으며, 작업 지시가 완료 처리되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);

            }

        }



        private bool CanExecuteAddCommand() => true; // 지금은 항상 등록 가능하도록 true 반환



        private void ExecuteAddCommand()
        {
            // 1. 새 DTO 인스턴스를 만들고, Edit ViewModel에 넘겨줍니다. (Id=0)
            var newDto = new WorkOrderDto { Id = 0, ItemCode = "", Quantity = 0, Status = "w" };
            var editVm = new WorkOrderEditViewModel(newDto);
            //OpenEditWindowRequested?.Invoke(editVm, "작업지시등록");
            if (OpenEditWindowRequested != null)
            {
                OpenEditWindowRequested.Invoke(editVm, "작업지시등록");
            }
            // --- 수정된 부분: IsSaved와 IsValid를 모두 확인 ---
            if (editVm.IsSaved)
            {
                // 유효성 검사를 통과했을 경우 DB 저장
                var savedDto = editVm.CommitChanges();
                _service.CreateWorkOrder(savedDto);
                ExecuteLoadCommandAsync();

                /*

                // --- 수정된 부분: 저장 전 Validate 호출 및 메시지 박스 표시 ---

                if (editVm.Validate())

                {

                    

                }

                else

                {

                    // 유효성 검사 실패 시 사용자에게 경고 메시지 표시

                    MessageBox.Show(editVm.ValidationMessage, "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                }

                // -----------------------------------------------------------------*/

                MessageBox.Show("정상적으로 등록 되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);

            }



            // DB 저장 후, 목록을 다시 로드하여 최신 상태를 반영합니다.

            //ExecuteLoadCommandAsync();

        }



        private void ExecuteEditCommand()

        {

            if (SelectedWorkOrder == null) return;



            var editVm = new WorkOrderEditViewModel(SelectedWorkOrder);



            OpenEditWindowRequested?.Invoke(editVm, "작업 지시 수정");



            if (editVm.IsSaved)

            {

                // 유효성 검사를 통과했을 경우

                var index = WorkOrders.IndexOf(SelectedWorkOrder);

                var updatedDto = editVm.CommitChanges();



                if (index >= 0)

                {

                    WorkOrders.RemoveAt(index);

                    WorkOrders.Insert(index, updatedDto);

                    SelectedWorkOrder = updatedDto;

                }



                _service.UpdateWorkOrder(updatedDto);

                /*

                // --- 수정된 부분: 저장 전 Validate 호출 및 메시지 박스 표시 ---

                if (editVm.Validate())

                {

                    

                }

                else

                {

                    // 유효성 검사 실패 시 경고 메시지 표시

                    MessageBox.Show(editVm.ValidationMessage, "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                }

                // -----------------------------------------------------------------*/

            }

        }



        private void ExecuteDeleteCommand()

        {

            if (SelectedWorkOrder == null) return;



            _service.DeleteWorkOrder(SelectedWorkOrder.Id);

            ExecuteLoadCommandAsync();



            MessageBox.Show("정상적으로 삭제 되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);

        }



        // ---------------------------------------------------------------------

        // 5. Command Execution Methods (실행 로직) - 추가

        // ---------------------------------------------------------------------



        // 선택된 작업 지시가 '대기' 상태일 때만 시작 가능

        private bool CanExecuteStartWorkCommand()

            => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Wait;



        private void ExecuteStartWorkCommand()

        {

            if (SelectedWorkOrder == null) return;



            // 1. Service를 호출하여 DB 상태를 'Processing'으로 업데이트

            _service.UpdateWorkOrderStatus(SelectedWorkOrder.Id, WorkOrderStatus.Processing);



            // 2. UI 목록의 해당 DTO를 강제 업데이트

            // DTO를 새로고침하거나, 리스트를 다시 로드해야 UI에 반영됩니다.

            ExecuteLoadCommandAsync();

        }



        // 선택된 작업 지시가 '진행 중' 상태일 때만 완료 가능

        private bool CanExecuteCompleteWorkCommand()

            => SelectedWorkOrder != null && SelectedWorkOrder.StatusEnum == WorkOrderStatus.Processing;



        private void ExecuteCompleteWorkCommand()

        {

            if (SelectedWorkOrder == null) return;



            // 1. Service를 호출하여 DB 상태를 'Complete'로 업데이트

            _service.UpdateWorkOrderStatus(SelectedWorkOrder.Id, WorkOrderStatus.Complete);



            // 2. UI 목록 갱신

            ExecuteLoadCommandAsync();

        }





        // ---------------------------------------------------------------------

        // 6. INotifyPropertyChanged 구현 (UI 갱신 알림)

        // ---------------------------------------------------------------------



        // 속성 값이 변경될 때 발생하는 이벤트입니다. XAML 바인딩 시스템이 이 이벤트를 듣습니다.

        public event PropertyChangedEventHandler PropertyChanged;



        // Protected 메서드: 이 메서드를 호출하여 View에게 속성 값 변경을 공식적으로 알립니다.

        protected void OnPropertyChanged(string propertyName)

        {

            // 이벤트 구독자(View)가 있다면, 속성 이름(propertyName)을 담아 이벤트를 발생시킵니다.

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }
}