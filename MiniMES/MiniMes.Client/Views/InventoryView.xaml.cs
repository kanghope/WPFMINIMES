using MiniMes.Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniMes.Client.Views
{
    /// <summary>
    /// InventoryView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 데이터 그리드 정렬 시 로딩 표시 처리
        /// </summary>
        private async void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (DataContext is InventoryViewModel viewmodel)
            {
                // 1. 로딩 상태 켜기
                viewmodel.IsBusy = true;
                viewmodel.StatisticsSummary = "데이터 정렬 중...";

                // 2. UI가 로딩 표시를 그릴 수 있도록 양보
                await Task.Delay(10);

                // 3. 백그라운드 우선순위로 정렬 수행
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        e.Handled = false; // 기본 정렬 로직 실행 허용
                    }
                    finally
                    {
                        viewmodel.IsBusy = false;
                        // 정렬 후 통계 요약은 유지되도록 처리 (필요시 재계산 호출 가능)
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}
