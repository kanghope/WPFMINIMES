using System;
using System.Windows;
// ... 기타 네임스페이스 생략 ...
using MiniMes.Client.ViewModels;

namespace MiniMes.Client.Views
{
    /// <summary>
    /// [View] 실적 등록 화면 클래스
    /// 이 클래스는 화면(XAML)과 데이터(ViewModel) 사이에서 
    /// 창 열기/닫기 같은 '화면 자체의 동작'을 조절하는 역할을 합니다.
    /// </summary>
    public partial class WorkResultRegisterView : Window
    {
        // ---------------------------------------------------------------------
        // 1. 생성자: 창이 만들어질 때 실행됨
        // ---------------------------------------------------------------------
        public WorkResultRegisterView()
        {
            // XAML에 그려놓은 버튼, 텍스트박스 등 UI 요소들을 실제로 생성합니다.
            InitializeComponent();
        }

        // ---------------------------------------------------------------------
        // 2. 저장 버튼 클릭 시 동작
        // ---------------------------------------------------------------------
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // DataContext(화면에 연결된 데이터 뭉치)가 WorkResultRegisterViewModel인지 확인합니다.
            // 맞다면 'vm'이라는 이름으로 가져와서 사용합니다.
            if (DataContext is WorkResultRegisterViewModel vm)
            {
                // [Step 1] 입력한 데이터가 올바른지 검사 (예: 수량이 0은 아닌지 등)
                // vm 내부의 Validate() 함수를 호출하여 확인합니다.
                if (vm.Validate())
                {
                    // [Step 2] 실제로 DB에 저장하라는 명령(RegisterCommand)을 실행합니다.
                    // Execute(null)은 버튼을 누른 것과 같은 효과를 코드로 낸 것입니다.
                    vm.RegisterCommand.Execute(null);

                    // [Step 3] 저장이 성공했는지 확인
                    // ViewModel에서 저장이 잘 끝나면 IsSaved를 true로 바꿔주도록 설계되어 있습니다.
                    if (vm.IsSaved)
                    {
                        // DialogResult = true는 이 창을 연 부모 창에게 "성공적으로 끝났어!"라고 신호를 보내는 것입니다.
                        this.DialogResult = true;
                        // 현재 창을 닫습니다.
                        this.Close();
                    }
                    // 만약 저장이 실패했다면(vm.IsSaved가 false라면), 창을 닫지 않고 사용자가 수정할 기회를 줍니다.
                }
                else
                {
                    // [Step 4] 유효성 검사 실패 시
                    // ViewModel에 저장된 에러 메시지를 사용자에게 알림창으로 보여줍니다.
                    MessageBox.Show(vm.ValidationMessage, "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // ---------------------------------------------------------------------
        // 3. 취소 버튼 클릭 시 동작
        // ---------------------------------------------------------------------
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is WorkResultRegisterViewModel vm)
            {
                // 혹시 모르니 저장되지 않았음을 확실히 표시합니다.
                vm.IsSaved = false;
            }

            // DialogResult = false는 부모 창에게 "사용자가 취소했어"라고 신호를 보냅니다.
            this.DialogResult = false;
            // 창을 닫습니다.
            this.Close();
        }
    }
}