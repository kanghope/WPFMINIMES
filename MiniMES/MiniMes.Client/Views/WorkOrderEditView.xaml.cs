using MiniMes.Client.ViewModels;
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
    /// [작업지시 등록/수정 팝업창의 비하인드 코드]
    /// 사용자가 버튼을 눌렀을 때의 동작(창 닫기, 메시지 띄우기 등)을 담당합니다.
    /// </summary>
    public partial class WorkOrderEditView : Window
    {
        // 생성자: 창이 만들어질 때 실행됩니다.
        public WorkOrderEditView()
        {
            InitializeComponent(); // XAML 화면 디자인을 불러옵니다.
        }

        // ---------------------------------------------------------------------
        // [저장 버튼] 클릭 시 실행
        // ---------------------------------------------------------------------
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 이 화면에 연결된 '두뇌(DataContext)'가 WorkOrderEditViewModel인지 확인합니다.
            // 'vm'이라는 별명으로 부르기 시작합니다.
            if (DataContext is WorkOrderEditViewModel vm)
            {
                // 1. 유효성 검사 (ViewModel의 검문소를 통과해야 합니다)
                if (vm.Validate())
                {
                    // [통과 시 로직]
                    // ViewModel에 "이 데이터는 진짜로 저장할 거예요!"라고 신호를 줍니다.
                    vm.IsSaved = true;

                    // 이 창을 연 메인 화면(부모)에게 "작업 성공!"(true)이라고 알려줍니다.
                    this.DialogResult = true;

                    // 팝업창을 닫습니다.
                    this.Close();
                }
                else
                {
                    // [탈락 시 로직]
                    // ViewModel이 정리해둔 "뭐가 잘못됐는지" 메시지를 팝업으로 띄워줍니다.
                    MessageBox.Show(vm.ValidationMessage, "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // ---------------------------------------------------------------------
        // [취소 버튼] 클릭 시 실행
        // ---------------------------------------------------------------------
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 두뇌(ViewModel)를 가져옵니다.
            if (DataContext is WorkOrderEditViewModel viewModel)
            {
                // 저장하지 않을 것이므로 IsSaved를 false로 확실히 해둡니다.
                viewModel.IsSaved = false;
            }

            // 이 창을 연 메인 화면(부모)에게 "그냥 취소했어요"(false)라고 알려줍니다.
            this.DialogResult = false;

            // 아무 작업 없이 팝업창을 닫습니다.
            this.Close();
        }
    }
}