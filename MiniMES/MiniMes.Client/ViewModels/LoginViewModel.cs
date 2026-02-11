using MiniMES.Infrastructure.Auth;
using MiniMes.Infrastructure.Interfaces;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MiniMes.Client.Helpers;

namespace MiniMes.Client.ViewModels
{
    public class LoginViewModel :INotifyPropertyChanged
    {
        private readonly IAuthService _authService;


        // 아이디 입력 시마다 CanExecute를 다시 체크해야 하므로 RaiseCanExecuteChanged가 필요할 수 있습니다.
        private string _userId;
        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;

            // 2. 생성 시에도 RelayCommand<Window>라고 명시해야 합니다.
            // 만약 RelayCommand 클래스에 제네릭이 없는 버전이 없다면 반드시 형식을 써줘야 합니다.
            // 기존의 일반 RelayCommand 형식을 그대로 사용
            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);
        }

        // 버튼 활성화 조건: 아이디와 비밀번호가 비어있지 않아야 함
        private bool CanExecuteLogin() => !string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrWhiteSpace(Password);

        // 비동기 실행 로직
        private async Task ExecuteLoginAsync()
        {
            var user = await _authService.AuthenticateAsync(UserId, Password);

            if (user != null)
            {
                // 1. 세션 정보 먼저 저장
                UserSession.UserId = user.USER_ID;
                UserSession.UserName = user.USER_NAME;
                UserSession.UserRole = user.USER_ROLE;
                UserSession.LoginTime = DateTime.Now; // [추가] 로그인 시작 시간 기록

                // 2. 창 찾기 및 종료 처리
                // 여기서 핵심은 '찾은 즉시' DialogResult를 설정하고 루프를 빠져나가는 것입니다.
                var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MiniMes.Client.Views.LoginView);

                if (window != null)
                {
                    // [중요] DialogResult를 true로 설정하는 순간 ShowDialog() 블록이 해제됩니다.
                    window.DialogResult = true;
                    window.Close();
                }
            }
            else
            {
                MessageBox.Show("로그인 정보가 올바르지 않습니다.");
            }
        }

        // ---------------------------------------------------------------------
        // 6. 알림 인터페이스 (WPF의 핵심)
        // ---------------------------------------------------------------------
        public event PropertyChangedEventHandler? PropertyChanged;

        // 프로퍼티의 값이 바뀌었을 때 화면(XAML)에 "야! 데이터 바뀌었으니까 다시 그려!"라고 
        // 신호를 보내는 확성기 같은 메서드입니다.
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
