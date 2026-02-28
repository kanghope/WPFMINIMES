using MiniMes.Client.Helpers;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using MiniMES.Infastructure.interfaces;
using MiniMES.Infastructure.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MiniMes.Client.ViewModels
{
    /// <summary>
    /// 품목 마스터 관리 화면을 위한 ViewModel
    /// 주요 기능: 품목 조회(필터링), 신규 추가, 저장(수정), 삭제
    /// </summary>
    public class ItemManagementViewModel : BaseViewModel
    {
        // [1. 도구들] 인터페이스 기반 서비스 및 비동기 제어 도구
        private readonly IItemService _itemService; // DB 통신을 담당하는 서비스
        private CancellationTokenSource? _loadCts;  // 연속 조회 시 이전 작업을 취소하기 위한 도구

        // [2. 데이터 저장소] 화면의 DataGrid와 바인딩될 컬렉션
        private ObservableCollection<ItemDto> _items = new ObservableCollection<ItemDto>();
        public ObservableCollection<ItemDto> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(nameof(Items)); }
        }

        // 선택된 품목 (DataGrid에서 사용자가 마우스로 클릭한 행)
        private ItemDto? _selectedItem;
        public ItemDto? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                // 선택된 항목이 바뀌면 '저장', '삭제' 버튼의 사용 가능 여부(CanExecute)를 다시 계산하도록 신호를 보냄
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // [상태 관리 변수] 로딩바, 프로그래스바, 하단 통계 메시지용
        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); } }

        private double _loadingProgress;
        public double LoadingProgress { get => _loadingProgress; set { _loadingProgress = value; OnPropertyChanged(nameof(LoadingProgress)); } }

        private string _statisticsSummary = "데이터 대기 중...";
        public string StatisticsSummary { get => _statisticsSummary; set { _statisticsSummary = value; OnPropertyChanged(nameof(StatisticsSummary)); } }

        // [검색 조건 필터]
        private string _searchText = string.Empty; // 검색 텍스트박스 바인딩
        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(nameof(SearchText)); } }

        private string _selectedTypeFilter = "ALL"; // 품목 구분 콤보박스 바인딩 (전체, 완제품, 원자재)
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set { _selectedTypeFilter = value; OnPropertyChanged(nameof(SelectedTypeFilter)); }
        }

        // [3. 버튼 명령] XAML의 Button Command에 바인딩됨
        public ICommand SearchCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        // [5. 생성자] 객체가 생성될 때 초기화 수행
        // 기본 생성자: 디자인 타임이나 간단한 생성 시 ItemService를 주입
        public ItemManagementViewModel() : this(new ItemService()) { }

        // 의존성 주입(DI)을 받는 생성자: 단위 테스트 및 실제 런타임에 사용
        public ItemManagementViewModel(IItemService itemService)
        {
            _itemService = itemService;

            // [핵심] 다른 스레드(Background)에서 Items 컬렉션을 수정해도 UI 스레드와 자동으로 동기화되도록 설정
            BindingOperations.EnableCollectionSynchronization(Items, new object());

            // 명령(Command)과 실제 실행할 함수(Method)를 연결
            SearchCommand = new RelayCommand(async () => await ExecuteSearchAsync());
            LoadCommand = new RelayCommand(async () => await ExecuteLoadCommandAsync());
            AddCommand = new RelayCommand(ExecuteAddCommand);
            // 저장과 삭제는 '선택된 항목이 있을 때만' 활성화되도록 CanExecute 조건 추가
            SaveCommand = new RelayCommand(async () => await ExecuteSaveCommandAsync(), CanExecuteSave);
            DeleteCommand = new RelayCommand(async () => await ExecuteDeleteCommandAsync(), CanExecuteDelete);

            // 화면이 처음 열릴 때 자동으로 데이터를 한 번 조회 (비주얼 스튜디오 디자이너 모드 제외)
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _ = ExecuteLoadCommandAsync();
            }
        }

        // [6. 버튼 활성화 조건 체크]
        private bool CanExecuteSave() => SelectedItem != null;   // 행을 선택해야 저장 버튼 활성화
        private bool CanExecuteDelete() => SelectedItem != null; // 행을 선택해야 삭제 버튼 활성화

        // [7. 실제 실행 로직 (Action Methods)]

        // 검색 버튼 클릭 시
        private async Task ExecuteSearchAsync()
        {
            await ExecuteLoadCommandAsync();
        }

        // 데이터를 불러오는 핵심 로직
        private async Task ExecuteLoadCommandAsync()
        {
            if (_isLoading) return; // 이미 데이터를 가져오는 중이면 중복 실행 방지

            // 이전에 실행 중이던 비동기 작업이 있다면 취소 요청
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            try
            {
                IsLoading = true; // 화면에 로딩 오버레이 표시
                StatisticsSummary = "품목 데이터를 불러오는 중...";
                LoadingProgress = 0;

                // 1. 서비스로부터 전체 품목 리스트를 비동기로 가져옴
                var allData = await _itemService.GetAllItemsAsync();

                // 2. 검색 조건(코드/이름) 및 필터(품목구분)에 맞게 데이터 필터링
                var filteredData = allData.Where(x =>
                    (string.IsNullOrEmpty(_searchText) || x.ItemCode.Contains(_searchText) || x.ItemName.Contains(_searchText)) &&
                    (_selectedTypeFilter == "ALL" || x.ItemType == _selectedTypeFilter)
                ).ToList();

                Items.Clear(); // 기존 리스트 비우기
                double totalCount = filteredData.Count;

                // 3. [배치 로딩] 데이터가 많을 경우 UI가 멈추지 않도록 500개씩 나누어 리스트에 추가
                int batchSize = 500;
                for (int i = 0; i < filteredData.Count; i += batchSize)
                {
                    if (token.IsCancellationRequested) return; // 작업이 취소되었다면 중단

                    var batch = filteredData.Skip(i).Take(batchSize).ToList();

                    // UI 스레드에 작업 요청 (Background 순위로 실행하여 사용자 조작 우선순위 보장)
                    await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (var item in batch) Items.Add(item);

                        // 프로그래스바 퍼센트 계산
                        if (totalCount > 0)
                            LoadingProgress = ((double)(i + batch.Count) / totalCount) * 100;
                    }), System.Windows.Threading.DispatcherPriority.Background);

                    StatisticsSummary = $"{i + batch.Count:N0}개 품목 로드 중...";
                    await Task.Delay(1); // UI 스레드가 다른 이벤트를 처리할 시간을 줌
                }

                // 4. 로드 완료 후 하단 통계 정보 갱신
                int fgCount = filteredData.Count(x => x.ItemType == "FG"); // 완제품 수
                int rmCount = filteredData.Count(x => x.ItemType == "RM"); // 원자재 수
                StatisticsSummary = $"총 품목: {totalCount:N0}건 (완제품: {fgCount}, 원자재: {rmCount})";
                LoadingProgress = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"품목 로드 중 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false; // 로딩 종료
            }
        }

        // [신규] 유효성 검사 전용 메서드
        private bool ValidateItem(ItemDto item, out string message)
        {
            // 1. 필수값 체크 (품목코드)
            if (string.IsNullOrWhiteSpace(item.ItemCode) || item.ItemCode == "NEW_CODE")
            {
                message = "올바른 품목 코드를 입력해주세요.";
                return false;
            }

            // 2. 필수값 체크 (품목명)
            if (string.IsNullOrWhiteSpace(item.ItemName))
            {
                message = "품목 명칭은 비워둘 수 없습니다.";
                return false;
            }

            // 3. 중복 체크 (신규 추가인 경우에만 리스트 내 중복 확인)
            // 실제 업무에서는 DB에서도 한 번 더 체크해야 하지만, UI단에서 먼저 걸러줍니다.
            var duplicateCount = Items.Count(x => x.ItemCode == item.ItemCode);
            if (duplicateCount > 1)
            {
                message = "이미 리스트에 존재하는 품목 코드입니다.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        // '추가' 버튼 클릭 시: 그리드 맨 윗줄에 새로운 빈 데이터 행 생성
        private void ExecuteAddCommand()
        {
            // 이미 리스트에 'NEW_CODE'가 있다면 추가를 막음
            if (Items.Any(x => x.ItemCode == "NEW_CODE"))
            {
                MessageBox.Show("이미 편집 중인 신규 항목이 있습니다. 먼저 저장해주세요.");
                return;
            }

            var newItem = new ItemDto
            {
                ItemCode = "NEW_CODE",
                ItemName = "신규 품목",
                ItemType = "RM",
                IsActive = true
            };
            Items.Insert(0, newItem); // 리스트 최상단에 추가
            SelectedItem = newItem;   // 추가된 행을 자동으로 선택 상태로 만듦
        }

        // '저장' 버튼 클릭 시: 현재 선택된 행의 데이터를 DB에 반영
        private async Task ExecuteSaveCommandAsync()
        {
            if (SelectedItem == null) return;
            //유효성검사
            if(!ValidateItem(SelectedItem, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                // 서비스의 저장 로직 호출 (내부적으로 Insert 또는 Update 처리)
                bool success = await _itemService.SaveItemAsync(SelectedItem);

                if (success)
                {
                    MessageBox.Show("품목 정보가 저장되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    await ExecuteLoadCommandAsync(); // 변경된 데이터가 잘 들어갔는지 다시 조회
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // '삭제' 버튼 클릭 시
        private async Task ExecuteDeleteCommandAsync()
        {
            if (SelectedItem == null) return;

            // 사용자에게 다시 한번 확인 (실수로 삭제하는 것 방지)
            var result = MessageBox.Show($"[{SelectedItem.ItemCode}] 품목을 삭제하시겠습니까?\n(BOM이나 작업지시에 사용 중인 경우 실패할 수 있습니다.)",
                "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // DB에서 삭제 시도
                    bool success = await _itemService.DeleteItemAsync(SelectedItem.ItemCode);
                    if (success)
                    {
                        Items.Remove(SelectedItem); // 리스트에서도 제거
                        MessageBox.Show("삭제되었습니다.");
                    }
                }
                catch (Exception ex)
                {
                    // 외래키 제약 조건(이미 다른 테이블에서 참조 중) 등의 이유로 삭제 실패 시 예외 처리
                    MessageBox.Show($"삭제 실패 (외래키 제약 확인): {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


    }
}