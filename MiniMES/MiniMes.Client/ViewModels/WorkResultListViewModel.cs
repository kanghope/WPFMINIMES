using MiniMes.Client.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;



namespace MiniMes.Client.ViewModels

{

    // 특정 작업 지시에 대한 실적 목록을 관리하는 ViewModel

    public class WorkResultListViewModel : INotifyPropertyChanged

    {

        // ---------------------------------------------------------------------

        // 1. Dependency

        // ---------------------------------------------------------------------

        private readonly IWorkResultService _resultService;

        private readonly WorkOrderDto _workOrder; // 조회 기준이 될 작업 지시 정보



        // ---------------------------------------------------------------------

        // 2. Data Properties

        // ---------------------------------------------------------------------

        private ObservableCollection<WorkResultDto> _results = new ObservableCollection<WorkResultDto>();

        public ObservableCollection<WorkResultDto> Results

        {

            get => _results;

            set { _results = value; OnPropertyChanged(nameof(Results)); }

        }



        // UI 표시용 (선택된 WorkOrder 정보)

        public int WorkOrderId => _workOrder.Id;

        public string ItemCode => _workOrder.ItemCode;

        public int OrderQuantity => _workOrder.Quantity;

        public string WorkOrderStatus => _workOrder.DisplayStatus; // 상태 정보



        // ---------------------------------------------------------------------

        // 3. Command Properties

        // ---------------------------------------------------------------------

        public ICommand LoadResultsCommand { get; }



        // ---------------------------------------------------------------------

        // 4. Constructor (DI 적용)

        // ---------------------------------------------------------------------

        public WorkResultListViewModel(WorkOrderDto workOrder, IWorkResultService resultService)

        {

            _workOrder = workOrder;

            // DI 받은 IWorkResultService 할당

            _resultService = resultService ?? throw new ArgumentNullException(nameof(resultService));



            // Command 초기화

            LoadResultsCommand = new RelayCommand(async () => await ExecuteLoadResultsCommand());



            // 생성 시 즉시 데이터 로드

            _= ExecuteLoadResultsCommand();

        }



        // ---------------------------------------------------------------------

        // 5. Execution Logic

        // ---------------------------------------------------------------------

        private async Task ExecuteLoadResultsCommand()

        {

            Results.Clear();



            // Service를 호출하여 특정 WorkOrder ID에 대한 실적 목록을 가져옵니다.

            var data = await _resultService.GetResultsByWorkOrder(_workOrder.Id);



            data.ToList().ForEach(Results.Add);

        }



        // ---------------------------------------------------------------------

        // 6. INotifyPropertyChanged 구현

        // ---------------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)

        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }

}