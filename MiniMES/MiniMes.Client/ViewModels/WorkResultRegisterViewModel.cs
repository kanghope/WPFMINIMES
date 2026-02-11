using MiniMes.Client.Helpers;// RelayCommand 참조
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services; // WorkResultService 참조

namespace MiniMes.Client.ViewModels
{
    public class WorkResultRegisterViewModel : BaseViewModel

    {

        // [수정] 서비스를 인터페이스 타입으로 필드 선언

        //private readonly IWorkOrderService _workOrderService;
        //22222222222222222


        // [속성/필드 영역]
        // 실적을 저장할 때 사용할 실제 DB 서비스 객체입니다.
        // 실무에서는 생성자를 통해 주입(DI)받지만, 현재는 직접 생성을 통해 기능을 구현했습니다.
        private readonly WorkResultService _resultService; //= new WorkResultService();

        // 이전 화면(리스트)에서 선택하여 넘어온 '어떤 작업 지시에 대한 실적인지' 정보를 담고 있는 바구니입니다.
        private readonly WorkOrderDto _workOrder;

        // 실적 등록 창이 성공적으로 저장되고 닫혔는지를 판단하는 플래그입니다.
        // 이 값을 통해 부모 화면(리스트)은 데이터를 새로고침할지 결정합니다.
        public bool IsSaved { get; set; }

        // 아래 3개는 UI(View)에서 읽기 전용으로 작업 지시 정보를 보여주기 위한 속성입니다.
        public int WorkOrderId => _workOrder.Id;      // 작업 지시 고유 ID
        public string ItemCode => _workOrder.ItemCode;  // 생산 품목 코드
        public int OrderQuantity => _workOrder.Quantity; // 계획된 지시 수량

        // [사용자 입력 필드: 양품 수량]
        private int _goodQuantity;
        public int GoodQuantity
        {
            get { return _goodQuantity; }
            set
            {
                _goodQuantity = value;
                Validate(); // 값이 바뀔 때마다 수량이 적절한지 검사합니다.
                OnPropertyChanged(nameof(GoodQuantity)); // UI에 값이 바뀌었다고 알립니다.
            }
        }

        // [사용자 입력 필드: 불량 수량]
        private int _badQuantity;
        public int BadQuantity
        {
            get => _badQuantity;
            set
            {
                _badQuantity = value;
                Validate();
                OnPropertyChanged(nameof(BadQuantity));
            }
        }

        // [유효성 검사 메시지]
        // 수량이 잘못 입력되었을 때 사용자에게 "수량이 너무 많습니다" 등의 안내를 띄울 텍스트입니다.
        private string? _validationMessage;
        public string? ValidationMessage
        {
            get => _validationMessage;
            private set { _validationMessage = value; OnPropertyChanged(nameof(ValidationMessage)); }
        }



        // [버튼 리모컨 선언]
        // UI의 '등록' 버튼과 바인딩될 객체입니다. 
        // 단순히 클릭 이벤트뿐만 아니라 "이 버튼을 지금 누를 수 있는가?"까지 관리합니다.
        public ICommand RegisterCommand { get; }

        // 1. [기존 호환용 생성자] 
        // 기존에 호출하던 곳(Parameter 1개)에서 에러가 나지 않도록 유지합니다.
        public WorkResultRegisterViewModel(WorkOrderDto workOrder)
            : this(workOrder, new WorkResultService()) // 아래에 있는 2번 생성자를 호출합니다.
        {
            // 여기는 비워두어도 됩니다. 모든 로직은 2번 생성자에서 처리됩니다.
        }
        // [생성자: 객체 탄생 시점]
        public WorkResultRegisterViewModel(WorkOrderDto workOrder, WorkResultService resultService)
        {
            // 이전 화면으로부터 넘겨받은 원본 지시 데이터(작업번호, 품목 등)를 보관합니다.
            _workOrder = workOrder;
            _resultService = resultService;

            // 사용자가 숫자를 입력하기 전, 초기값을 0으로 안전하게 설정합니다.
            _goodQuantity = 0;
            _badQuantity = 0;

            // 아직 DB 저장이 안 되었으므로 false로 시작합니다.
            IsSaved = false;

            // [RelayCommand 생성]
            // 첫 번째 인자(ExecuteRegisterCommand): 버튼 클릭 시 실제로 실행할 함수
            // 두 번째 인자(CanExecuteRegisterCommand): 버튼 활성화 여부를 결정하는 함수 (true면 활성, false면 비활성)
            RegisterCommand = new RelayCommand(async () => await ExecuteRegisterCommand(), CanExecuteRegisterCommand);

            // 창이 뜨자마자 현재 상태(0, 0)가 올바른지 한 번 체크합니다.
            Validate();
        }


        // [버튼 활성화 조건 결정]
        // 이 함수가 false를 반환하면 UI의 버튼은 자동으로 회색(비활성) 상태가 됩니다.
        // Validate() 결과가 성공(true)일 때만 버튼을 누를 수 있게 하여 잘못된 데이터 입력을 원천 차단합니다.
        private bool CanExecuteRegisterCommand() => Validate();

        // [버튼 클릭 시 실행되는 실제 로직]
        private async Task ExecuteRegisterCommand()
        {
            // 1. [데이터 변환] 
            // ViewModel에 흩어져 있는 정보(ID, 양품, 불량)를 DB 저장용 포맷인 DTO 객체로 하나로 묶습니다.
            var resultDto = new WorkResultDto
            {
                WorkOrderId = _workOrder.Id,
                GoodQuantity = this.GoodQuantity,
                BadQuantity = this.BadQuantity,
                ResultDate = DateTime.Now // 서버 시간이 아닌 현재 로컬 저장 시간을 기록합니다.
            };

            try
            {
                // 2. [서비스 호출] 
                // 서비스 계층에 데이터를 전달하여 실제 DB Insert를 수행합니다.
                // 내부적으로는 '실적 저장'과 '작업지시 상태 변경(진행중->완료)'이 트랜잭션으로 묶여있을 것입니다.
                await _resultService.RegisterWorkResult(resultDto);

                // 저장이 성공하면 플래그를 true로 바꿔서 부모 창에 성공을 알립니다.
                IsSaved = true;
            }
            catch (Exception ex)
            {
                // 3. [예외 처리]
                // 네트워크 장애, DB 제약조건 위반 등 에러 발생 시 사용자에게 팝업을 띄웁니다.
                // 이때 IsSaved는 false로 유지되어 부모 창이 새로고침되지 않도록 합니다.
                MessageBox.Show($"실적 등록 중 오류 발생: {ex.Message}", "DB 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                IsSaved = false;
            }
        }



        // [데이터 검증 로직]
        // MES 시스템에서 데이터의 무결성(Integrity)을 유지하는 가장 중요한 함수입니다.
        public bool Validate()
        {
            // 새로운 검사를 위해 기존 에러 메시지를 비웁니다.
            ValidationMessage = string.Empty;

            // 검사 1: 상식 밖의 숫자(음수) 입력 방지
            if (GoodQuantity < 0 || BadQuantity < 0)
            {
                ValidationMessage += "수량은 0 미만일 수 없습니다.\n";
            }

            // 검사 2: 과생산 방지 (현장 업무 규칙)
            // 실제 생산된 총합이 지시 수량을 넘어가면 경고를 줍니다.
            if (GoodQuantity + BadQuantity > OrderQuantity)
            {
                ValidationMessage += $"총 실적 수량({GoodQuantity + BadQuantity})이 지시 수량({OrderQuantity})보다 많습니다.\n";
            }

            // [반환값] 에러 메시지가 하나도 없다면(Empty) true를 반환하여 '유효함'을 알립니다.
            return string.IsNullOrEmpty(ValidationMessage);
        }


        // ---------------------------------------------------
        // [WPF의 핵심: UI 갱신 신호]
        // ---------------------------------------------------

        // 속성 값이 바뀔 때 UI에 "너도 바뀌어야 해!"라고 소리치는 이벤트입니다.
        //public event PropertyChangedEventHandler? PropertyChanged;

        //// 이 함수가 호출되면 WPF의 데이터 바인딩 시스템이 해당 이름의 UI 요소를 다시 그립니다.
        //protected void OnPropertyChanged(string propertyName)
        //{
        //    // 구독자(UI)가 있을 때만 이벤트를 발생시킵니다.
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
}