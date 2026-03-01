using MiniMes.Domain.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiniMes.Domain.DTOs
{
    /// <summary>
    /// BOM 정보를 화면(UI)과 서비스 간에 전달하는 데이터 객체입니다.
    /// INotifyPropertyChanged를 구현하여 프로퍼티 값이 변경될 때마다 
    /// UI(DataGrid 등)에 즉시 알림을 보내 화면을 갱신합니다.
    /// </summary>
    public class BomDto : INotifyPropertyChanged
    {
        // DB의 고유 식별자 (변경 알림이 굳이 필요 없는 고정값)
        public int BomId { get; set; }

        // 부모 품목 코드 (조회 시 결정되는 값)
        public string ParentItemCode { get; set; } = string.Empty;

        // --- 변경 알림이 필요한 속성들 (Backing Field 방식) ---

        private string _childItemCode = string.Empty;
        /// <summary>
        /// 자재 코드: 사용자가 DataGrid에 코드를 입력하면 
        /// OnPropertyChanged에 의해 UI가 이 변경을 감지합니다.
        /// </summary>
        public string ChildItemCode
        {
            get => _childItemCode;
            set
            {
                _childItemCode = value;
                OnPropertyChanged(); // 값이 바뀌었다고 UI에 "종"을 울림
            }
        }

        private string _childItemName;
        /// <summary>
        /// 자재명: UI에서는 ReadOnly(읽기전용)지만, 
        /// 코드를 통해 값이 입력되면 화면에 즉시 표시되어야 하므로 알림이 필요합니다.
        /// </summary>
        public string ChildItemName
        {
            get => _childItemName;
            set { _childItemName = value; OnPropertyChanged(); }
        }

        private string _childItemSpec;
        /// <summary>
        /// 규격: 프로그램이 마스터 정보를 조회해 넣어줄 때 화면 갱신을 수행합니다.
        /// </summary>
        public string ChildItemSpec
        {
            get => _childItemSpec;
            set { _childItemSpec = value; OnPropertyChanged(); }
        }

        private string _childItemUnit;
        /// <summary>
        /// 단위: 프로그램이 마스터 정보를 조회해 넣어줄 때 화면 갱신을 수행합니다.
        /// </summary>
        public string ChildItemUnit
        {
            get => _childItemUnit;
            set { _childItemUnit = value; OnPropertyChanged(); }
        }

        private decimal _consumption;
        /// <summary> 단위 소요량 (제품 1개당 필요한 양) </summary>
        public decimal Consumption
        {
            get => _consumption;
            set
            {
                _consumption = value;
                OnPropertyChanged();
                UpdateCalculatedFields();
            }
        }

        private decimal _currentStock;
        /// <summary> 현재 창고 재고 </summary>
        public decimal CurrentStock
        {
            get => _currentStock;
            set
            {
                _currentStock = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsShortage));
            }
        }

        private decimal _parentOrderQty;
        /// <summary> 작업지시 수량 (부모 품목을 얼마나 만들 것인가) </summary>
        public decimal ParentOrderQty
        {
            get => _parentOrderQty;
            set
            {
                _parentOrderQty = value;
                OnPropertyChanged();
                UpdateCalculatedFields();
            }
        }

        /// <summary> 총 필요 수량 (단위 소요량 * 지시 수량) </summary>
        public decimal TotalRequiredQty => Consumption * ParentOrderQty;

        /// <summary> 재고 부족 여부 (현재고 < 총 필요수량) </summary>
        public bool IsShortage => CurrentStock < TotalRequiredQty;

        /// <summary> 부족한 수량 (화면에 표시용) </summary>
        public decimal ShortageQty => IsShortage ? TotalRequiredQty - CurrentStock : 0;


        // --- 유틸리티 메서드 ---
        private void UpdateCalculatedFields()
        {
            OnPropertyChanged(nameof(TotalRequiredQty));
            OnPropertyChanged(nameof(IsShortage));
            OnPropertyChanged(nameof(ShortageQty));
        }

        // --- MVVM 패턴의 핵심: PropertyChanged 이벤트 처리 ---

        /// <summary>
        /// 프로퍼티 값이 변경되었음을 알리는 이벤트입니다.
        /// WPF의 Binding 시스템이 이 이벤트를 구독(Subscribe)합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 속성 변경 이벤트를 발생시키는 메서드입니다.
        /// [CallerMemberName]을 사용하면 호출한 속성의 이름(예: "ChildItemName")을
        /// 자동으로 인자로 전달받아 오타 실수를 방지합니다.
        /// </summary>
        /// <param name="name">변경된 속성명</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            // 이벤트를 구독 중인 UI 요소들에게 "name" 속성이 바뀌었으니 다시 그리라고 명령함
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}