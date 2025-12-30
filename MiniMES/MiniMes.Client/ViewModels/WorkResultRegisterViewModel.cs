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
    public class WorkResultRegisterViewModel : INotifyPropertyChanged

    {

        // [수정] 서비스를 인터페이스 타입으로 필드 선언

        //private readonly IWorkOrderService _workOrderService;
       //1111111


        // DB 서비스

        private readonly WorkResultService _resultService = new WorkResultService(); // 의존성 주입 (간소화)
        //private readonly IWorkResultService _resultService;




        // WorkOrderListViewModel로부터 전달받은 작업 지시 정보

        private readonly WorkOrderDto _workOrder;

        // 팝업이 저장되었는지 플래그

        public bool IsSaved { get; set; }



        // UI 표시용

        public int WorkOrderId => _workOrder.Id;

        public string ItemCode => _workOrder.ItemCode;

        public int OrderQuantity => _workOrder.Quantity;



        // 실적 입력 필드

        private int _goodQuantity;

        public int GoodQuantity

        {

            get { return _goodQuantity; }

            set { _goodQuantity = value; Validate(); OnPropertyChanged(nameof(GoodQuantity)); }

        }



        private int _badQuantity;

        public int BadQuantity

        {

            get => _badQuantity;

            set { _badQuantity = value; Validate(); OnPropertyChanged(nameof(BadQuantity)); }

        }



        // 유효성 검사 메시지

        private string _validationMessage;

        public string ValidationMessage

        {

            get => _validationMessage;

            private set { _validationMessage = value; OnPropertyChanged(nameof(ValidationMessage)); }

        }



        // Command

        public ICommand RegisterCommand { get; }



        public WorkResultRegisterViewModel(WorkOrderDto workOrder)

        {
             //_resultService = resultService;
             _workOrder = workOrder;

            _goodQuantity = 0; // 초기값 설정
            _badQuantity = 0;
            IsSaved = false;

            RegisterCommand = new RelayCommand(ExecuteRegisterCommand, CanExecuteRegisterCommand);

            Validate(); // 초기 유효성 검사
            
        }



        // ---------------------------------------------------

        // Command 실행 로직

        // ---------------------------------------------------



        private bool CanExecuteRegisterCommand() => Validate(); // 유효할 때만 저장 가능



        private void ExecuteRegisterCommand()
        {
            // 1. DTO 생성
            var resultDto = new WorkResultDto
            {
                WorkOrderId = _workOrder.Id,
                GoodQuantity = this.GoodQuantity,
                BadQuantity = this.BadQuantity,
                ResultDate = DateTime.Now
            };

            try
            {
                // 2. 서비스 호출: 실적 등록 및 WO 상태 완료 처리
                _resultService.RegisterWorkResult(resultDto);
                IsSaved = true;
            }
            catch (Exception ex)
            {
                // DB 저장 실패 시 처리
                MessageBox.Show($"실적 등록 중 오류 발생: {ex.Message}", "DB 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                IsSaved = false;
            }
        }



        // ---------------------------------------------------

        // 유효성 검사

        // ---------------------------------------------------



        public bool Validate()
        {
            ValidationMessage = string.Empty;

            if (GoodQuantity < 0 || BadQuantity < 0)
            {
                ValidationMessage += "수량은 0 미만일 수 없습니다.\n";
            }

            // 총 생산 수량 (양품 + 불량)이 지시 수량보다 많으면 경고
            if (GoodQuantity + BadQuantity > OrderQuantity)
            {
                ValidationMessage += $"총 실적 수량({GoodQuantity + BadQuantity})이 지시 수량({OrderQuantity})보다 많습니다.\n";
            }

            // 필수 입력 확인은 없으므로, ValidationMessage가 비어있지 않으면 유효성 실패
            return string.IsNullOrEmpty(ValidationMessage);
        }



        // ---------------------------------------------------

        // INotifyPropertyChanged 구현

        // ---------------------------------------------------



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)

        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }

}