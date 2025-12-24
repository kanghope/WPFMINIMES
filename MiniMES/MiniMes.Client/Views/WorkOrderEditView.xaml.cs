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

using MiniMes.Client.ViewModels;



namespace MiniMes.Client.Views

{

    /// <summary>

    /// Window1.xaml에 대한 상호 작용 논리

    /// </summary>

    public partial class WorkOrderEditView : Window

    {

        public WorkOrderEditView()

        {

            InitializeComponent();

        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)

        {

            // DataContext를 ViewModel 타입으로 가져옵니다.

            // DataContext가 Edit ViewModel인지 확인

            if (DataContext is WorkOrderEditViewModel vm)

            {

                // 1. 유효성 검사 실행

                if (vm.Validate())

                {

                    // 2. 검사 통과: IsSaved를 true로 설정하고 창을 닫습니다.

                    vm.IsSaved = true;

                    this.DialogResult = true;// WorkOrderListViewModel로 성공을 알림

                    this.Close();

                }

                else

                {

                    // 3. 검사 실패: 경고 메시지 박스를 띄웁니다.

                    MessageBox.Show(vm.ValidationMessage, "입력오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                }

                //vm.IsSaved = true; // 저장 성공 플래그 설정

            }

            //this.Close(); // 팝업 창 닫기

        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)

        {

            // 취소 버튼: IsSaved=false (기본값)로 두고 창을 닫습니다.

            // DataContext가 ViewModel일 때만 처리

            if (DataContext is WorkOrderEditViewModel viewModel)

            {

                viewModel.IsSaved = false;

            }

            this.DialogResult = false; // WorkOrderListViewModel로 실패/취소를 알림

            this.Close();

        }

    }

}