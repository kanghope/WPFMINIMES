using MiniMES.Infrastructure.Auth;
using MiniMes.Client.Views;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Linq;
using MiniMes.Client; // 프로젝트 이름이 MiniMes.Client인 경우
// 또는 
using App = MiniMes.Client.App; // 이름이 겹칠 경우 명시적으로 지정

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public BaseViewModel()
    {
        // 생성자에서는 값만 체크하고, 
        // 실제 UI를 띄우는 Redirect 로직은 Dispatcher로 비동기 처리하는 것이 안전합니다.
        if (this.GetType().Name != "LoginViewModel" && !UserSession.IsLoggedIn)
        {
            // UI 스레드 차단을 막기 위해 비동기로 호출
            Application.Current.Dispatcher.BeginInvoke(new Action(() => RedirectToLogin()));
        }
    }

    private void RedirectToLogin()
    {
        MessageBox.Show("세션이 만료되었거나 로그인이 필요합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);

        // 2. 현재 열려있는 모든 창(메인화면 등)을 찾아서 닫습니다.
        var windows = Application.Current.Windows.Cast<Window>().ToList();

        // 3. DI 컨테이너에서 로그인 창을 새로 가져옵니다.
        var loginView = App.ServiceProvider?.GetRequiredService<LoginView>();

        if (loginView != null)
        {
            // 새 로그인 창을 띄우고 성공 여부를 확인합니다.
            if (loginView.ShowDialog() == true && UserSession.IsLoggedIn)
            {
                // 로그인 성공 시 메인 화면을 다시 띄우고 싶다면 
                // App.xaml.cs의 OnStartup 로직을 타게 되므로 여기서는 창만 닫아주면 됩니다.
                windows.ForEach(w => w.Close());
            }
            else
            {
                // 로그인 창에서 취소를 눌렀다면 앱 종료
                Application.Current.Shutdown();
            }
        }
    }

    // 권한 확인용 헬퍼 메서드
    protected bool CheckAdmin() => UserSession.UserRole == "ADMIN";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}