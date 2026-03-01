using MiniMes.Client.Helpers;
using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities; // 필요시
using MiniMES.Infastructure.interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading; // CancellationTokenSource를 위해 필요
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiniMes.Client.ViewModels
{
    public class InventoryViewModel : BaseViewModel
    {
        private readonly IStockRepository _stockRepository;

        // [추가] 연속 클릭 시 이전 작업을 취소하기 위한 도구
        private CancellationTokenSource? _loadCts;

        // ---------------------------------------------------------------------
        // 1. UI 바인딩 속성
        // ---------------------------------------------------------------------
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); }
        }

        public ObservableCollection<StockDto> StockList { get; set; } = new ObservableCollection<StockDto>();

        // [추가] 통계 및 로딩 상태 표시용
        private string _statisticsSummary = "데이터를 불러올 준비가 되었습니다.";
        public string StatisticsSummary
        {
            get => _statisticsSummary;
            set { _statisticsSummary = value; OnPropertyChanged(nameof(StatisticsSummary)); }
        }

        private double _loadingProgress;
        public double LoadingProgress
        {
            get => _loadingProgress;
            set { _loadingProgress = value; OnPropertyChanged(nameof(LoadingProgress)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        // ---------------------------------------------------------------------
        // 2. 명령(Commands)
        // ---------------------------------------------------------------------
        public ICommand SearchCommand { get; }
        public ICommand SaveInboundCommand { get; }
        public ICommand RefreshCommand { get; }

        public InventoryViewModel(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;

            SearchCommand = new RelayCommand(async () => await LoadStockData());
            RefreshCommand = new RelayCommand(async () => {
                SearchText = string.Empty;
                await LoadStockData();
            });
            SaveInboundCommand = new RelayCommand<StockDto>(async (item) => await ExecuteInbound(item));

           
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _ = LoadStockData();
            }
        }

        // ---------------------------------------------------------------------
        // 3. 고성능 데이터 로드 로직 (핵심 수정 부분)
        // ---------------------------------------------------------------------
        private async Task LoadStockData()
        {
            // 1. [중복 방지] 이미 로딩 중이면 무시합니다.
            if (IsBusy) return;

            // 2. [취소 토큰] 이전 작업이 돌고 있다면 취소 신호를 보냅니다.
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            try
            {
                IsBusy = true; // 로딩 시작 (UI 오버레이 표시)
                StatisticsSummary = "재고 데이터를 불러오는 중...";
                LoadingProgress = 0;

                // 3. [비동기 조회] DB에서 데이터를 가져옵니다. (UI 스레드 영향 없음)
                var data = await _stockRepository.GetStockListAsync(SearchText);
                var dataList = data.ToList();
                double totalCount = dataList.Count;

                // 중간에 사용자가 다른 검색을 눌렀다면 여기서 중단!
                if (token.IsCancellationRequested) return;

                StockList.Clear();

                // 4. [병렬 처리] CPU를 많이 쓰는 통계 계산은 백그라운드 스레드에서 미리 돌립니다.
                var statsTask = Task.Run(() =>
                {
                    var totalStockQty = dataList.Sum(x => x.CurrentQty);
                    var outOfStockCount = dataList.Count(x => x.CurrentQty <= 0);
                    return $"총 재고량: {totalStockQty:N0} | 품절/부족 품목: {outOfStockCount:N0}건";
                }, token);

                // 5. [Batch 로드] 2,000건씩 끊어서 UI 리스트에 추가 (화면 멈춤 방지)
                int batchSize = 1000;
                for (int i = 0; i < dataList.Count; i += batchSize)
                {
                    if (token.IsCancellationRequested) return;

                    var batch = dataList.Skip(i).Take(batchSize).ToList();

                    // UI 스레드에게 "화면 그리는 거 방해 안 할 테니까 여유 날 때 추가해줘"라고 부탁 (Background 우선순위)
                    await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (var item in batch) StockList.Add(item);

                        // 진행률 계산
                        if (totalCount > 0)
                        {
                            LoadingProgress = ((double)(i + batch.Count) / totalCount) * 100;
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);

                    StatisticsSummary = $"{i + batch.Count:N0}건 로딩 중...";

                    // UI 스레드가 메시지 큐를 처리할 시간을 줌
                    await Task.Delay(1);
                }

                // 6. 결과 반영
                StatisticsSummary = await statsTask;
                LoadingProgress = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"재고 조회 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false; // 반드시 로딩 해제
            }
        }

        private async Task ExecuteInbound(StockDto item)
        {
            if (item == null || item.InboundQty <= 0) return;

            var result = MessageBox.Show($"[{item.ItemName}] {item.InboundQty}개를 입고하시겠습니까?",
                "입고", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _stockRepository.UpdateStockInboundAsync(item.ItemCode, item.InboundQty);
                    await LoadStockData(); // 다시 고성능 로직으로 로드
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"저장 실패: {ex.Message}");
                }
            }
        }
    }
}