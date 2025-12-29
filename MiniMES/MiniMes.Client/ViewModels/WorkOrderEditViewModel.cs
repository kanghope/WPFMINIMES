using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using MiniMes.Domain.DTOs;

namespace MiniMes.Client.ViewModels
{
    // 등록/수정 팝업창의 두뇌 역할을 하는 ViewModel입니다.
    public class WorkOrderEditViewModel : INotifyPropertyChanged
    {
        // [_originalDto] 진짜 원본 주문서입니다. (수정이 끝나기 전까진 건드리지 않아요)
        private readonly WorkOrderDto _originalDto;

        // [IsSaved] 사용자가 '저장' 버튼을 눌러서 성공적으로 끝났는지 확인하는 신호등입니다.
        public bool IsSaved { get; set; }

        // [IsValid] 입력한 내용이 올바른지(예: 수량이 0은 아닌지) 알려주는 상태 플래그입니다.
        private bool _isValid = true;
        public bool IsValid
        {
            get => _isValid;
            private set { _isValid = value; OnPropertyChanged(nameof(IsValid)); }
        }

        // [IsItemCodeReadOnly] 수정 모드일 때 "품목 코드는 못 고치게" 막아주는 잠금장치입니다.
        private bool _isItemCodeReadOnly;
        public bool IsItemCodeReadOnly
        {
            get { return _isItemCodeReadOnly; }
            private set { _isItemCodeReadOnly = value; OnPropertyChanged(nameof(IsItemCodeReadOnly)); }
        }

        // ---------------------------------------------------------------------
        // 화면(UI)과 연결된 "임시 입력 칸"들입니다.
        // ---------------------------------------------------------------------

        public int Id => _originalDto.Id; // 주문 번호는 고칠 수 없으니 가져오기만 합니다.

        private string _itemCode;
        public string ItemCode
        {
            get { return _itemCode; }
            // 글자를 타이핑할 때마다 실행되어 화면을 갱신합니다.
            set { _itemCode = value; OnPropertyChanged(nameof(ItemCode)); }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        // [ValidationMessage] "수량을 입력하세요" 같은 경고 문구를 저장하는 통입니다.
        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set { _validationMessage = value; OnPropertyChanged(nameof(ValidationMessage)); }
        }

        // ---------------------------------------------------------------------
        // 생성자: 창이 열릴 때 처음 실행됩니다.
        // ---------------------------------------------------------------------
        public WorkOrderEditViewModel(WorkOrderDto workOrder)
        {
            // 전달받은 원본 데이터를 보관합니다.
            _originalDto = workOrder;

            // 새 주문 등록(Id=0)인 경우
            if (workOrder.Id == 0)
            {
                ItemCode = workOrder.ItemCode;
                Quantity = workOrder.Quantity;
                IsItemCodeReadOnly = false; // 새로 등록하는 거니까 코드를 마음껏 쓸 수 있어요.
            }
            // 기존 주문 수정(Id > 0)인 경우
            else
            {
                ItemCode = workOrder.ItemCode;
                Quantity = workOrder.Quantity;
                IsItemCodeReadOnly = true; // [중요] 이미 있는 주문은 품목 코드를 못 바꾸게 잠급니다.
            }

            IsSaved = false; // 아직 저장 전이니까 false!
            Validate();      // 처음 켰을 때도 입력값이 정상인지 한 번 검사합니다.
        }

        // ---------------------------------------------------------------------
        // [Validate] 입력값이 올바른지 검사하는 "검문소" 메서드입니다.
        // ---------------------------------------------------------------------
        public bool Validate()
        {
            // 1. 품목 코드가 비어있지 않은지 검사
            bool itemCodeValid = !string.IsNullOrWhiteSpace(ItemCode);
            // 2. 수량이 0보다 큰지 검사
            bool quantityValid = Quantity > 0;

            ValidationMessage = string.Empty; // 이전 경고 메시지는 싹 지웁니다.

            if (!itemCodeValid)
            {
                ValidationMessage += "품목 코드는 필수 입력 사항입니다.\n";
            }
            if (!quantityValid)
            {
                ValidationMessage += "지시 수량은 1 이상의 값이어야 합니다.\n";
            }

            // 메시지 통이 비어있으면 "정상", 하나라도 적혀있으면 "오류"입니다.
            return string.IsNullOrEmpty(ValidationMessage);
        }

        // ---------------------------------------------------------------------
        // [CommitChanges] "최종 승인!" 임시 입력 칸의 내용을 원본 DTO로 옮깁니다.
        // ---------------------------------------------------------------------
        public WorkOrderDto CommitChanges()
        {
            // 임시로 적어둔 값들을 진짜 원본 종이(_originalDto)에 옮겨 적습니다.
            _originalDto.ItemCode = this.ItemCode;
            _originalDto.Quantity = this.Quantity;

            // 이제 이 원본을 들고 DB로 가서 저장하면 됩니다.
            return _originalDto;
        }

        // ---------------------------------------------------------------------
        // 화면 갱신 알림 기능 (표준 코드)
        // ---------------------------------------------------------------------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}