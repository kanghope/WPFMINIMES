using MiniMes.Client.Helpers;
using MiniMes.Client.Views;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using MiniMES.Infastructure.interfaces;
using MiniMES.Infastructure.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MiniMes.Client.ViewModels
{
    /// <summary>
    /// BOM(Bill of Materials) 관리 화면의 비즈니스 로직을 담당하는 ViewModel
    /// </summary>
    public class BomManagementViewModel : BaseViewModel
    {
        // [1. 의존성 주입 대상 서비스]
        private readonly IBomService _bomService;   // BOM 데이터(관계를 정의하는 테이블) 처리 서비스
        private readonly IItemService _itemService; // 품목 마스터(품명, 단위 등 기본 정보) 조회 서비스

        // [2. 화면 바인딩용 컬렉션 (DataGrid 연결)]
        // 왼쪽: 모든 품목 중 '완제품(FG)' 카테고리만 담는 리스트
        public ObservableCollection<ItemDto> ParentItems { get; } = new ObservableCollection<ItemDto>();

        // 오른쪽: 왼쪽에서 선택된 특정 완제품에 들어가는 '하위 자재 구성' 리스트
        public ObservableCollection<BomDto> BomDetails { get; } = new ObservableCollection<BomDto>();

        // [추가] 하단 ListBox와 바인딩될 로그 컬렉션
        public ObservableCollection<string> CommunicationLogs { get; } = new ObservableCollection<string>();

        // [추가] 로딩 바의 퍼센트 수치와 바인딩
        private double _loadingProgress;
        public double LoadingProgress
        {
            get => _loadingProgress;
            set { _loadingProgress = value; OnPropertyChanged(nameof(LoadingProgress)); }
        }

        // [추가] 완제품 검색어 바인딩
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); }
        }

        // [3. 화면에서 선택된 객체 상태 관리]
        private ItemDto? _selectedParent;
        /// <summary>
        /// 왼쪽 DataGrid에서 사용자가 클릭한 완제품
        /// </summary>
        public ItemDto? SelectedParent
        {
            get => _selectedParent;
            set
            {
                _selectedParent = value;
                OnPropertyChanged(nameof(SelectedParent));

                // [중요 로직] 부모 품목이 바뀌면 그 아래 달린 BOM 자재 리스트를 즉시 다시 불러옵니다.
                // _ = 는 비동기 메서드를 기다리지 않고(Fire and Forget) 호출하겠다는 의미입니다.
                _ = LoadBomDetailsAsync();
            }
        }

        private BomDto? _selectedBom;
        /// <summary>
        /// 오른쪽 BOM 상세 리스트에서 사용자가 클릭한 행
        /// </summary>
        public BomDto? SelectedBom
        {
            get => _selectedBom;
            set
            {
                _selectedBom = value;
                OnPropertyChanged(nameof(SelectedBom));
                // 선택 상태가 변할 때마다 버튼(저장, 삭제 등)의 활성화 여부를 UI에 즉시 갱신하도록 요청
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // [4. UI 상태 관리]
        private bool _isLoading;
        /// <summary>
        /// 데이터 처리 중 로딩 애니메이션 표시 여부
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        // [5. 버튼 클릭과 연결되는 명령(Command)]
        public ICommand LoadParentsCommand { get; } // 완제품 목록 새로고침
        public ICommand AddDetailCommand { get; }    // 새로운 자재 행 추가
        public ICommand SaveDetailCommand { get; }   // 현재 편집 중인 BOM 저장
        public ICommand DeleteDetailCommand { get; } // 선택된 BOM 삭제

        // 1. 팝업 호출 커맨드 정의
        public ICommand OpenItemPopupCommand { get; } 
        // [6. 생성자]
        public BomManagementViewModel() : this(new BomService(), new ItemService()) { }

        public BomManagementViewModel(IBomService bomService, IItemService itemService)
        {
            _bomService = bomService;
            _itemService = itemService;

            // 명령(Command) 객체 초기화 및 실행 메서드 연결
            LoadParentsCommand = new RelayCommand(async () => await LoadParentItemsAsync());

            // AddDetail: 완제품(Parent)이 선택되어 있을 때만 실행 가능
            AddDetailCommand = new RelayCommand(ExecuteAddDetail, () => SelectedParent != null);

            // Save/Delete: 편집할 BOM 상세 항목(SelectedBom)이 선택되어 있을 때만 실행 가능
            SaveDetailCommand = new RelayCommand(async () => await ExecuteSaveDetailAsync(), () => SelectedBom != null);
            DeleteDetailCommand = new RelayCommand(async () => await ExecuteDeleteDetailAsync(), () => SelectedBom != null);

            // [수정] RelayCommand<BomDto>를 사용하여 DataGrid의 특정 행을 파라미터로 전달받음
            OpenItemPopupCommand = new RelayCommand<BomDto>(ExecuteOpenItemPopup);

            // [디자인 타임 방어] 실제 런타임에서만 데이터를 자동으로 로드하도록 설정
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _ = LoadParentItemsAsync();
            }
        }
        /// <summary>
        /// 품목 선택 팝업창을 실행합니다.
        /// </summary>
        /// <param name="targetRow">DataGrid에서 🔍 버튼을 누른 해당 행의 데이터</param>
        private void ExecuteOpenItemPopup(BomDto targetRow)
        {
            // 행 데이터가 없으면 현재 선택된 SelectedBom을 대안으로 사용
            var row = targetRow ?? SelectedBom;
            if (row == null) return;

            // 1. 팝업 창과 뷰모델 객체 생성
            var popupView = new ItemSelectView();
            var popupVm = new ItemSelectViewModel(_itemService);

            // 2. 팝업 뷰모델의 닫기 콜백 설정
            popupVm.RequestClose = () => {
                popupView.DialogResult = true; // ShowDialog()가 true를 반환하도록 함
                popupView.Close();
            };

            popupView.DataContext = popupVm;

            // 부모 창 설정 (팝업이 부모창 중앙에 뜨게 함)
            popupView.Owner = Application.Current.MainWindow;

            // 3. 모달 창 표시
            if (popupView.ShowDialog() == true)
            {
                // 4. 사용자가 선택한 아이템 정보를 현재 행에 매핑
                var selected = popupVm.SelectedItem;
                if (selected != null)
                {
                    row.ChildItemCode = selected.ItemCode;
                    row.ChildItemName = selected.ItemName;
                    row.ChildItemSpec = selected.ItemSpec;
                    row.ChildItemUnit = selected.ItemUnit;

                    AddLog($"[자재선택] {selected.ItemName} ({selected.ItemCode}) 적용 완료");
                }
            }
        }

        // [7. 실제 실행 로직들]

        /// <summary>
        /// (왼쪽 리스트) DB에서 전체 품목을 가져와 '완제품(FG)'만 걸러내어 보여줍니다.
        /// </summary>
        private async Task LoadParentItemsAsync()
        {
            IsLoading = true;
            LoadingProgress = 0;
            AddLog("완제품 목록 조회 시작...");

            try
            {
                var items = await _itemService.GetAllItemsAsync();
                ParentItems.Clear();

                // 검색어가 있으면 필터링, 없으면 전체 FG 로드
                var filteredItems = items.Where(x => x.ItemType == "FG" );
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filteredItems = filteredItems.Where(x =>
                        x.ItemCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        x.ItemName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                foreach (var item in filteredItems)
                    ParentItems.Add(item);

                LoadingProgress = 100;
                AddLog($"조회 완료: {ParentItems.Count}건의 완제품이 로드되었습니다.");
            }
            catch (Exception ex)
            {
                AddLog($"오류 발생: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        /// <summary>
        /// (오른쪽 리스트) 선택된 완제품 코드를 기준으로 연결된 BOM 자재 목록을 DB에서 조회합니다.
        /// </summary>
        private async Task LoadBomDetailsAsync()
        {
            // 선택된 부모가 없으면 리스트를 비우고 종료
            if (SelectedParent == null) { BomDetails.Clear(); return; }

            IsLoading = true;
            try
            {
                // 서비스 호출: TB_BOM 테이블에서 PARENT_ITEM이 현재 부모 코드인 행들을 Join하여 가져옴
                var details = await _bomService.GetBomListByParentAsync(SelectedParent.ItemCode);
                BomDetails.Clear();
                foreach (var detail in details) BomDetails.Add(detail);
            }
            finally { IsLoading = false; }
        }

        /// <summary>
        /// '자재 추가' 버튼: 입력할 수 있는 빈 행을 리스트 맨 위에 삽입합니다.
        /// </summary>
        private void ExecuteAddDetail()
        {
            if (SelectedParent == null) return;

            var newBom = new BomDto
            {
                ParentItemCode = SelectedParent.ItemCode,
                ChildItemCode = "코드 선택 →", // 사용자가 🔍 버튼을 누르도록 유도
                Consumption = 1
            };

            BomDetails.Insert(0, newBom);
            SelectedBom = newBom;
            AddLog("새 자재 행이 추가되었습니다. 🔍 버튼을 눌러 자재를 선택하세요.");
        }

        /// <summary>
        /// '저장' 버튼: 화면의 DTO 정보를 실제 DB 엔티티로 변환하여 저장합니다.
        /// </summary>
        private async Task ExecuteSaveDetailAsync()
        {
            if (SelectedBom == null) return;

            // [DTO -> Entity 매핑]
            // 화면에서 사용하는 DTO를 DB 테이블 구조인 Entity로 변환합니다.
            var entity = new MiniMes.Domain.Entities.BomEntity
            {
                BomId = SelectedBom.BomId,                 // 0이면 Insert, 아니면 Update
                ParentItemCode = SelectedBom.ParentItemCode,
                ChildItemCode = SelectedBom.ChildItemCode,
                Consumption = SelectedBom.Consumption      // 소요량
            };

            // DB 저장 시도
            bool success = await _bomService.SaveBomAsync(entity);
            if (success)
            {
                MessageBox.Show("BOM 정보가 저장되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                // 저장 후 리스트를 다시 조회하여 자재명(ItemName) 등의 조인 정보를 갱신
                await LoadBomDetailsAsync();
            }
        }

        /// <summary>
        /// '삭제' 버튼: 선택된 BOM 관계 데이터를 DB에서 제거합니다.
        /// </summary>
        private async Task ExecuteDeleteDetailAsync()
        {
            // 아직 DB에 저장되지 않은 신규 행(BomId == 0)인 경우
            if (SelectedBom == null || SelectedBom.BomId == 0)
            {
                if (SelectedBom != null) BomDetails.Remove(SelectedBom); // 화면 리스트에서만 제거
                return;
            }

            // DB에 실제 존재하는 데이터인 경우 사용자 확인 후 삭제
            if (MessageBox.Show("선택한 자재 구성을 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // DB에서 삭제 성공 시 화면 리스트에서도 제거
                if (await _bomService.DeleteBomAsync(SelectedBom.BomId))
                {
                    BomDetails.Remove(SelectedBom);
                }
            }
        }


        // 로직 보완 예시: 로그 기록 메서드
        private void AddLog(string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                CommunicationLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                // 너무 많은 로그가 쌓이지 않도록 관리
                if (CommunicationLogs.Count > 100) CommunicationLogs.RemoveAt(100);
            });
        }
    }
}
