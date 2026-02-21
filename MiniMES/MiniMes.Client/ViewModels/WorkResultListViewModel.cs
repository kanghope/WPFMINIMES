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
    /// <summary>
    /// [ViewModel] 실적 목록 조회 팝업 또는 상세 화면의 데이터 모델
    /// 특정 작업 지시(WorkOrder) 하나를 기준으로 그에 발생한 모든 생산 데이터(WorkResult)를 관리함.
    /// </summary>
    public class WorkResultListViewModel : BaseViewModel
    {
        // ---------------------------------------------------------------------
        // 1. Dependency (의존성 관리)
        // ---------------------------------------------------------------------
        // 실적 데이터를 DB나 API에서 가져오기 위한 비즈니스 로직 인터페이스
        private readonly IWorkResultService _resultService;

        // 이 화면의 기준이 되는 작업 지시서 정보 (예: 지시번호 101번에 대한 실적만 보겠다)
        private readonly WorkOrderDto _workOrder;

        // ---------------------------------------------------------------------
        // 2. Data Properties (UI 바인딩용 속성)
        // ---------------------------------------------------------------------

        // ObservableCollection: 목록이 추가/삭제될 때 UI(DataGrid 등)에 즉각 반영되는 컬렉션
        private ObservableCollection<WorkResultDto> _results = new ObservableCollection<WorkResultDto>();
        public ObservableCollection<WorkResultDto> Results
        {
            get => _results;
            set { _results = value; OnPropertyChanged(nameof(Results)); }
        }

        // [읽기 전용 속성] 상단 UI에 "현재 조회 중인 작업 정보"를 표시하기 위해 사용
        // 부모 객체(_workOrder)의 데이터를 직접 노출하여 가독성을 높임
        public int WorkOrderId => _workOrder.Id;
        public string ItemCode => _workOrder.ItemCode;
        public int OrderQuantity => _workOrder.Quantity;
        public string WorkOrderStatus => _workOrder.DisplayStatus; // '생산중', '완료' 등의 텍스트 정보

        // ---------------------------------------------------------------------
        // 3. Command Properties (UI 이벤트 연결)
        // ---------------------------------------------------------------------
        // 조회 버튼 클릭 시 실행될 명령
        public ICommand LoadResultsCommand { get; }

        // ---------------------------------------------------------------------
        // 4. Constructor (생성자 - DI 적용)
        // ---------------------------------------------------------------------
        /// <param name="workOrder">조회 대상이 되는 작업 지시 데이터</param>
        /// <param name="resultService">실적 데이터를 가져올 서비스 클래스</param>
        public WorkResultListViewModel(WorkOrderDto workOrder, IWorkResultService resultService)
        {
            _workOrder = workOrder;

            // Null Check: 서비스가 주입되지 않으면 프로그램 오류이므로 예외를 발생시킴
            _resultService = resultService ?? throw new ArgumentNullException(nameof(resultService));

            // RelayCommand: ExecuteLoadResultsCommand 메서드를 버튼 클릭과 연결
            LoadResultsCommand = new RelayCommand(async () => await ExecuteLoadResultsCommand());

            // [참고] 만약 화면이 뜨자마자 데이터를 보여주고 싶다면 아래 주석을 해제합니다.
            // _ = ExecuteLoadResultsCommand();
        }

        // ---------------------------------------------------------------------
        // 5. Execution Logic (데이터 처리 로직)
        // ---------------------------------------------------------------------
        /// <summary>
        /// 실제 DB로부터 실적 목록을 비동기로 로드하는 핵심 로직
        /// </summary>
        public async Task ExecuteLoadResultsCommand()
        {
            // 1. 기존 리스트 초기화 (새로고침 효과)
            Results.Clear();

            try
            {
                // 2. 현재 작업 지시 ID를 매개변수로 넘겨 실적 데이터를 가져옴 (비동기 호출)
                var data = await _resultService.GetResultsByWorkOrder(_workOrder.Id);

                // 3. 가져온 데이터를 ObservableCollection에 하나씩 추가
                // ToList().ForEach는 간결하지만 데이터가 많을 경우 foreach 루프가 더 빠를 수 있음
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        Results.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                // 실무 환경에서는 로그를 남기거나 사용자에게 알림창을 띄우는 로직 추가 권장
                System.Diagnostics.Debug.WriteLine($"실적 로드 실패: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------------
        // 6. INotifyPropertyChanged 구현 (BaseViewModel에서 상속받으므로 중복 제거됨)
        // ---------------------------------------------------------------------
        // BaseViewModel에 구현된 OnPropertyChanged를 사용하여 코드 중복을 피함
    }
}