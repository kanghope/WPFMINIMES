using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using MiniMes.Domain.DTOs;

namespace MiniMes.Client.ViewModels
{
    // 등록/수정 팝업을 위한 전용 ViewModel
    public class WorkOrderEditViewModel : INotifyPropertyChanged
    {
        // 원본 DTO를 백업해두는 필드 (수정 취소 시 되돌리기 위해)
        private readonly WorkOrderDto _originalDto;
        // 팝업이 닫힐 때, 저장이 성공했는지 메인 ViewModel에 알리기 위한 플래그

        public bool IsSaved { get; set; }



        // --- 1. 유효성 상태 플래그 추가 ---

        private bool _isValid = true;

        public bool IsValid

        {

            get => _isValid;

            private set { _isValid = value; OnPropertyChanged(nameof(IsValid)); }

        }

        // ------------------------------------

        // --- [추가] 품목 코드 활성화/비활성화 제어 속성 ---

        private bool _isItemCodeReadOnly;



        /// <summary>품목 코드 입력 상자의 읽기 전용 여부 (수정 모드일 때 true)</summary>

        public bool IsItemCodeReadOnly

        {

            get { return _isItemCodeReadOnly; }

            private set { _isItemCodeReadOnly = value; OnPropertyChanged(nameof(IsItemCodeReadOnly)); }

        }



        // **DTO의 속성들을 직접 ViewModel의 속성으로 노출** (UI 바인딩 대상)

        public int Id => _originalDto.Id;//// ID는 ReadOnly



        private string _itemCode;

        public string ItemCode

        {

            //get => _itemCode;

            get { return _itemCode; }

            set { _itemCode = value; OnPropertyChanged(nameof(ItemCode)); }

        }

        private int _quantity;

        public int Quantity

        {

            get => _quantity;

            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }

        }



        // --- 1. 유효성 검사 메시지 속성 추가 ---

        private string _validationMessage;



        // UI에 경고 메시지를 보여줄 Public 속성

        public string ValidationMessage

        {

            get => _validationMessage;

            private set { _validationMessage = value; OnPropertyChanged(nameof(ValidationMessage)); }

        }

        // ----------------------------------------



        public WorkOrderEditViewModel(WorkOrderDto workOrder)

        {

            // 원본 DTO를 저장

            _originalDto = workOrder;



            // 등록(Id=0)일 때 초기값 설정 (이전 단계에서 수정한 내용 유지)

            if (workOrder.Id == 0)

            {

                // 초기 값 설정 (복사)

                ItemCode = workOrder.ItemCode;

                Quantity = workOrder.Quantity;

                IsItemCodeReadOnly = false; // 등록 시에는 수정 가능

            }

            // 수정(Id>0)일 때 기존 값 설정

            else

            {

                ItemCode = workOrder.ItemCode;

                Quantity = workOrder.Quantity;

                IsItemCodeReadOnly = true; // [핵심] 수정 시에는 수정 불가능 (읽기 전용)

            }



            IsSaved = false;



            Validate();

        }



        // --- 2. Validate() 메서드 수정 (메시지 반환) ---

        // 이 메서드는 이제 메시지를 설정하고, 유효성 결과를 bool로 반환합니다.

        public bool Validate()

        {

            bool itemCodeValid = !string.IsNullOrWhiteSpace(ItemCode);

            bool quantityValid = Quantity > 0;



            ValidationMessage = string.Empty; // 메시지 초기화



            if (!itemCodeValid)

            {

                ValidationMessage += "품목 코드는 필수 입력 사항입니다.\n";

            }

            if (!quantityValid)

            {

                ValidationMessage += "지시 수량은 1 이상의 값이어야 합니다.\n";

            }



            // ValidationMessage가 비어있으면 유효함

            return string.IsNullOrEmpty(ValidationMessage);

        }

        // ------------------------------------------------



        // **저장 시 호출되어 실제 DTO에 변경사항을 반영하는 메서드**

        public WorkOrderDto CommitChanges()

        {

            // 이 시점에는 이미 ViewModel에서 IsSaved가 true일 때만 호출되므로,

            // 별도의 Validate() 호출은 WorkOrderListViewModel에서 관리합니다.

            _originalDto.ItemCode = this.ItemCode;

            _originalDto.Quantity = this.Quantity;

            return _originalDto;

        }

        // --- INotifyPropertyChanged 구현 ---

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)

        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }





    }

}