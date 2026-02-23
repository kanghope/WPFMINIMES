using MiniMES.Infastructure.Services;
using MiniMes.Domain.DTOs;
using System.Threading.Tasks;
using System.ComponentModel; // INotifyPropertyChanged를 쓰기 위해 필요해요!

namespace MiniMes.Client.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly SerialDeviceService _plcService;
        private readonly DashboardService _dbService;
        // UI에 바인딩될 속성
        private int _totalGood;
        public int TotalGood
        {
            get => _totalGood;
            set { _totalGood = value; OnPropertyChanged(nameof(TotalGood)); }
        }

        private int _totalBad;
        public int TotalBad
        {
            get => _totalBad;
            set { _totalBad = value; OnPropertyChanged(nameof(TotalBad)); }
        }

        private int _activeOrderCount;
        public int ActiveOrderCount
        {
            get => _activeOrderCount;
            set { _activeOrderCount = value; OnPropertyChanged(nameof(ActiveOrderCount)); }
        }

        private double _achievementRate;

        public double AchievementRate
        {
            get => _achievementRate;
            set { _achievementRate = value; OnPropertyChanged(nameof(AchievementRate)); }
        }

        // 생성자 주입 (DI)
        public DashboardViewModel(SerialDeviceService plcService, DashboardService dbService)
        {
            _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));

            // [핵심] 전역 서비스의 이벤트 구독
            // PLC 서비스가 데이터를 받아 DB 저장을 완료하면 이 이벤트를 쏩니다.
            _plcService.OnRefreshRequired += OnPlcDataRefreshed;

            // 처음 화면이 켜질 때 초기 데이터 로드
            _ = RefreshDashboard();
        }

        // [보완] 이벤트 핸들러 분리
        // 람다식보다 메서드로 분리하는 것이 추후 이벤트 해제(Unsubscribe) 시 유리합니다.
        private async void OnPlcDataRefreshed(bool needRefresh)
        {
            if (needRefresh)
            {
                // 실무 팁: PLC 수신은 백그라운드 스레드에서 일어납니다.
                // UI를 갱신할 때는 Dispatcher를 통해 UI 스레드로 작업을 보내주는 것이 가장 안전합니다.
                await App.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await RefreshDashboard();
                });
            }
        }

        private async Task RefreshDashboard()
        {
            try
            {
                var data = await _dbService.GetTodayProductionSummaryAsync();
                if (data != null)
                {
                    TotalGood = data.TotalGoodQty;
                    TotalBad = data.TotalBadQty; // 불량 수도 함께 업데이트
                    // AchievementRate 등 다른 DTO 속성도 여기서 매핑
                    ActiveOrderCount = data.ActiveOrderCount;
                    AchievementRate = data.AchievementRate;
                }
            }
            catch (Exception ex)
            {
                // 로그 기록 혹은 알림
                System.Diagnostics.Debug.WriteLine($"대시보드 갱신 오류: {ex.Message}");
            }
        }
    }
}
