using MiniMes.Client.ViewModels;
using MiniMes.Client.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MiniMes.Client.Views
{
    /// <summary>
    /// WorkOrderListView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkOrderListView : Window
    {
        public WorkOrderListView()
        {
            InitializeComponent();
            // ViewModel의 이벤트 구독// 창이 '로드' 되었을 때, ViewModel에게 데이터를 가져오라고 시킵니다.
            this.Loaded += (sender, e) =>
            {
                // 뷰모델을 찾아서 로드 명령을 내림
                if (DataContext is WorkOrderListViewModel viewmodel)
                {
                    viewmodel.OpenEditWindowRequested += OnOpenEditWindowRequested;
                    // [추가] 실적 등록 팝업 요청 이벤트 구독
                    viewmodel.OpenRegisterWindowRequested += OnOpenRegisterWindowRequested;
                    // [추가] 실적 조회 팝업 요청 이벤트 구독
                    viewmodel.OpenResultWindowRequested += OnOpenResultWindowRequested;
                }
            };
        }
        private void OnOpenEditWindowRequested(WorkOrderEditViewModel editVm, string title)
        {
            var editWindow = new WorkOrderEditView();
            editWindow.DataContext = editVm;
            editWindow.Title = title;
            
            // 모달 창 띄우기
            editWindow.ShowDialog();
        }

        // [추가] 실적 등록 팝업을 띄우는 핸들러 메서드
        private void OnOpenRegisterWindowRequested(WorkResultRegisterViewModel registerVm, string title)
        {
            var registerWindow = new WorkResultRegisterView(); // 새로운 팝업 뷰 인스턴스
            registerWindow.DataContext = registerVm;
            registerWindow.Title = title;
            registerWindow.ShowDialog();
        }

        // [추가] 실적 조회 팝업을 띄우는 핸들러 메서드
        private void OnOpenResultWindowRequested(WorkResultListViewModel resultVm, string title)
        {
            if (resultVm.Results.Any())
            {
                // 2. 데이터가 있을 경우: 정상적으로 팝업 뷰를 띄움
                var resultWindow = new WorkResultListView(); // WorkResultListView 인스턴스 생성
                // DataContext를 ViewModel 인스턴스(resultVm)로 설정
                resultWindow.DataContext = resultVm;
                resultWindow.Title = title;
                
                // 모달 창 띄우기
                resultWindow.ShowDialog();
            }
            else
            {
                // 3. 데이터가 없을 경우: 메시지 박스를 띄우고 종료
                // resultVm._workOrder에 접근할 수 없으므로, title에서 ItemCode를 추출하거나 ViewModel에 메시지용 속성을 추가하는 것이 좋으나, 
                // 여기서는 간단하게 "실적 정보가 없습니다"라고 표시합니다.
                MessageBox.Show($"선택된 작업 지시 ({resultVm.ItemCode})에 대해 등록된 실적 정보가 없습니다.",
                                "조회 실패",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
        }
    }
}