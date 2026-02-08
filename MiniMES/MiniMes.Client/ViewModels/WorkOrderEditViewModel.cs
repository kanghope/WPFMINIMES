using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // INotifyPropertyChanged를 쓰기 위해 필요해요!
using MiniMes.Domain.DTOs;

namespace MiniMes.Client.ViewModels
{
    /// <summary>
    /// [작업지시 등록/수정 화면용 비즈니스 로직]
    /// 이 클래스는 사용자가 팝업창에서 글자를 입력할 때, 그 값이 올바른지 체크하고
    /// 최종적으로 '저장'을 누르기 전까지 데이터를 임시로 붙잡고 있는 역할을 합니다.
    /// </summary>
    public class WorkOrderEditViewModel : INotifyPropertyChanged
    {
        // ---------------------------------------------------------------------
        // 1. 내부 저장소 (필드)
        // ---------------------------------------------------------------------

        // [_originalDto] 진짜 원본 주소입니다. 
        // 사용자가 입력을 하다가 '취소'를 누르면 원본이 망가지면 안 되기 때문에 
        // 따로 잘 모셔두는 용도입니다.
        private readonly WorkOrderDto _originalDto;

        // [IsSaved] 이 변수가 true가 되면, 메인 화면에게 "저장 버튼 눌렸어요! DB 새로고침 하세요!"라고 신호를 보냅니다.
        public bool IsSaved { get; set; }

        // [IsValid] 현재 입력한 값들이 DB에 들어가기에 적합한지(통과/탈락)를 나타냅니다.
        private bool _isValid = true;
        public bool IsValid
        {
            get => _isValid;
            private set { _isValid = value; OnPropertyChanged(nameof(IsValid)); }
        }

        // [IsItemCodeReadOnly] 
        // 등록할 때는 품목코드를 써야 하지만, 수정할 때는 품목코드를 못 고치게(읽기전용) 막아야 합니다.
        // UI(XAML)의 IsReadOnly 속성과 연결됩니다.
        private bool _isItemCodeReadOnly;
        public bool IsItemCodeReadOnly
        {
            get { return _isItemCodeReadOnly; }
            private set { _isItemCodeReadOnly = value; OnPropertyChanged(nameof(IsItemCodeReadOnly)); }
        }

        // ---------------------------------------------------------------------
        // 2. 화면(UI) 바인딩용 속성 (사용자가 보는 데이터)
        // ---------------------------------------------------------------------

        // [id] 작업지시의 고유 번호입니다. 고칠 수 없도록 get(읽기)만 만들었습니다.
        public int id
        {
            get { return _originalDto.Id; }
        }

        // [_itemCode] 사용자가 입력창에 타이핑하는 품목코드 문자열입니다.
        private string? _itemCode;
        public string? ItemCode
        {
            get { return _itemCode; }
            set
            {
                _itemCode = value;
                OnPropertyChanged(nameof(ItemCode)); // 화면에 "글자 바뀌었어!"라고 알림
                Validate(); // 글자를 칠 때마다 실시간으로 문법 검사 실행
            }
        }

        // [_quantity] 사용자가 입력하는 숫자(수량)입니다.
        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                Validate(); // 숫자를 바꿀 때마다 실시간으로 검사 실행
            }
        }

        // [ValidationMessage] 
        // 만약 수량을 -10이라고 쓰면, 화면 하단에 "1 이상 입력하세요"라고 띄워줄 글자 주머니입니다.
        private string? _validationMessage;
        public string? ValidationMessage
        {
            get => _validationMessage;
            private set { _validationMessage = value; OnPropertyChanged(nameof(ValidationMessage)); }
        }

        // ---------------------------------------------------------------------
        // 3. 생성자 (창이 딱 뜰 때 실행되는 설정창)
        // ---------------------------------------------------------------------
        public WorkOrderEditViewModel(WorkOrderDto workOrder)
        {
            // 전달받은 원본 데이터를 나중에 써먹기 위해 변수에 담아둡니다.
            _originalDto = workOrder;

            // [분기 처리] 이 팝업이 '새로 만들기'용인지 '수정'용인지 판단합니다.
            if (workOrder.Id == 0)
            {
                // [등록 모드] ID가 0이면 새로 만드는 것입니다.
                ItemCode = workOrder.ItemCode;
                Quantity = workOrder.Quantity;
                IsItemCodeReadOnly = false; // 새로 만드는 거니까 이름(코드)을 써야겠죠?
            }
            else
            {
                // [수정 모드] ID가 이미 있으면 기존 데이터를 고치는 것입니다.
                ItemCode = workOrder.ItemCode;
                Quantity = workOrder.Quantity;
                IsItemCodeReadOnly = true; // 실무에선 이미 등록된 주문의 품목코드는 고치지 못하게 막습니다.
            }

            IsSaved = false; // 처음엔 당연히 저장 전 상태입니다.
            Validate();      // 창이 뜨자마자 현재 값이 올바른지 한 번 훑어봅니다.
        }

        // ---------------------------------------------------------------------
        // 4. 유효성 검사 (검문소 로직)
        // ---------------------------------------------------------------------
        public bool Validate()
        {
            // [검사 조건 1] 품목 코드가 공백이거나 비어있으면 안 됩니다.
            bool itemCodeValid = !string.IsNullOrWhiteSpace(ItemCode);

            // [검사 조건 2] 지시 수량은 반드시 1개 이상이어야 합니다.
            bool quantityValid = Quantity > 0;

            // 일단 경고 메시지 주머니를 깨끗하게 비웁니다.
            ValidationMessage = string.Empty;

            // 조건에 걸리면 메시지를 추가합니다. (\n은 줄바꿈입니다)
            if (!itemCodeValid)
            {
                ValidationMessage += "● 품목 코드는 필수 입력 사항입니다.\n";
            }
            if (!quantityValid)
            {
                ValidationMessage += "● 지시 수량은 1 이상의 값이어야 합니다.\n";
            }

            // [최종 결과] 메시지 주머니가 비어있다면? -> 통과(true) / 메시지가 있다면? -> 탈락(false)
            IsValid = string.IsNullOrEmpty(ValidationMessage);
            return IsValid;
        }

        // ---------------------------------------------------------------------
        // 5. 변경사항 확정 (임시 -> 원본 복사)
        // ---------------------------------------------------------------------
        public WorkOrderDto CommitChanges()
        {
            // 사용자가 '확인'을 눌렀을 때만 실행됩니다.
            // 임시 변수에 담겨있던 값들을 진짜 원본(_originalDto) 객체에 덮어씌웁니다.
            _originalDto.ItemCode = this.ItemCode;
            _originalDto.Quantity = this.Quantity;

            // 이제 이 원본은 따끈따끈한 새 정보를 가지게 되었습니다.
            return _originalDto;
        }

        // ---------------------------------------------------------------------
        // 6. 알림 인터페이스 (WPF의 핵심)
        // ---------------------------------------------------------------------
        public event PropertyChangedEventHandler? PropertyChanged;

        // 프로퍼티의 값이 바뀌었을 때 화면(XAML)에 "야! 데이터 바뀌었으니까 다시 그려!"라고 
        // 신호를 보내는 확성기 같은 메서드입니다.
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}