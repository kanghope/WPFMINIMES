using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

using System.Windows.Input;// WPF Command 관련 인터페이스(ICommand)를 사용하기 위해 필요합니다.



namespace MiniMes.Client.Helpers

{

    // RelayCommand 클래스는 ICommand 인터페이스를 구현(implements)합니다.

    // ICommand는 WPF 컨트롤(Button 등)이 '명령'으로 인식하기 위한 표준 계약입니다.

    public class RelayCommand : ICommand

    {

        // '실행할 로직'을 담는 변수입니다. ViewModel에서 넘겨준 메서드(함수)가 여기에 저장됩니다.

        private readonly Action _execute;



        // '실행 가능한지 여부'를 판단하는 로직을 담는 변수입니다. 

        // 이 함수가 true를 반환하면 버튼은 활성화되고, false를 반환하면 비활성화됩니다.

        private readonly Func<bool>? _canExecute;



        // ---------------------------------------------------------------------

        // ICommand가 요구하는 멤버 1: CanExecuteChanged 이벤트

        // ---------------------------------------------------------------------



        // 버튼의 활성화/비활성화 상태를 다시 확인할 필요가 생겼을 때 WPF 시스템에 알립니다.
        /*
         * public: 외부(특히 WPF의 버튼들)에서 이 벨소리를 들을 수 있게 공개합니다.

event: "사건"이라는 뜻이죠. 어떤 일이 일어났을 때만 신호를 보낸다는 약속입니다.

EventHandler: 신호를 보낼 때 사용하는 규격(표준 양식)입니다.

CanExecuteChanged: 이름 그대로 **"실행 가능 여부(CanExecute)가 변경(Changed)되었음"**을 뜻합니다.
         */
        public event EventHandler CanExecuteChanged

        {

            // 이벤트가 추가될 때 (명령이 등록될 때)

            add { CommandManager.RequerySuggested += value; }

            // 이벤트가 제거될 때

            remove { CommandManager.RequerySuggested -= value; }

        }



        // ---------------------------------------------------------------------

        // 생성자: RelayCommand 객체를 만들 때, 실행할 로직을 받아 초기화합니다.

        // ---------------------------------------------------------------------



        public RelayCommand(Action execute, Func<bool>? canExecute = null)

        {

            // 실행 로직은 반드시 제공되어야 합니다. (없으면 오류 발생)

            _execute = execute ?? throw new ArgumentNullException(nameof(execute));



            // 실행 가능 여부 판단 로직은 선택 사항입니다.

            _canExecute = canExecute;

        }



        // ---------------------------------------------------------------------

        // ICommand가 요구하는 멤버 2: CanExecute 메서드 (실행 가능 여부 확인)

        // ---------------------------------------------------------------------



        // WPF 시스템이 버튼을 그릴 때 이 메서드를 호출하여 활성화할지 판단합니다.

        public bool CanExecute(object parameter) =>

            // 1. _canExecute 로직이 있으면 그 로직을 실행하여 결과를 반환합니다.
            _canExecute?.Invoke()
            // 2. _canExecute 로직이 없다면 (null 이라면) 무조건 true를 반환합니다.
            ?? true;
        /*
         public bool CanExecute(object parameter)
        {
            // 1. 만약 판단 기준(_canExecute)이 비어있다면?
            if (_canExecute == null)
            {
                // 판단할 기준이 없으니 "언제든 눌러도 된다"는 뜻으로 true를 돌려줍니다.
                return true;
            }
            else
            {
                // 2. 판단 기준이 있다면, 그 함수를 실행(.Invoke)해서 나온 결과를 돌려줍니다.
                // (예: 선택된 게 있으면 true, 없으면 false)
                return _canExecute.Invoke();
            }
        }
         */


        // ---------------------------------------------------------------------
        // ICommand가 요구하는 멤버 3: Execute 메서드 (로직 실행)
        // ---------------------------------------------------------------------
        // View에서 버튼이 '클릭'되었을 때 WPF 시스템에 의해 호출되는 최종 실행 메서드입니다.
        public void Execute(object parameter) =>
            // 생성자에서 저장해 둔 실제 ViewModel의 메서드(_execute)를 실행합니다.
            _execute.Invoke();

    }

}