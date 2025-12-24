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

using MiniMes.Client.ViewModels;

namespace MiniMes.Client.Views

{

    /// <summary>

    /// WorkResultRegisterView.xaml에 대한 상호 작용 논리

    /// </summary>

    public partial class WorkResultRegisterView : Window

    {

        public WorkResultRegisterView()

        {

            InitializeComponent();

        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)

        {

            if (DataContext is WorkResultRegisterViewModel vm)

            {

                // 1. 유효성 검사 실행 (바인딩된 IsEnabled 속성이 있지만, 최종 확인)

                if (vm.Validate())

                {

                    // 2. ViewModel의 저장 명령 실행

                    vm.RegisterCommand.Execute(null);



                    // 3. 저장이 성공했으면 창 닫기

                    if (vm.IsSaved)

                    {

                        this.DialogResult = true;

                        this.Close();

                    }

                    // 저장이 실패했으면 (catch에서 MessageBox 띄움), 창을 닫지 않고 유지

                }

                else

                {

                    // 유효성 검사 실패 메시지는 XAML TextBlock에 바인딩되어 보이므로, 추가 메시지 박스 생략 가능

                    MessageBox.Show(vm.ValidationMessage, "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                }

            }

        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)

        {

            if (DataContext is WorkResultRegisterViewModel vm)

            {

                vm.IsSaved = false; // 저장하지 않았음을 명시

            }

            this.DialogResult = false;

            this.Close();

        }

    }

}