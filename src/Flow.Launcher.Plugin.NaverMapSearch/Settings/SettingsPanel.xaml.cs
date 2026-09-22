using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.NaverMapSearch.Settings;

public partial class SettingsPanel : UserControl
{
    private readonly CredentialStore store;
    private readonly Action<string> openUrl;

    public SettingsPanel(CredentialStore store, Action<string> openUrl)
    {
        InitializeComponent();
        this.store = store;
        this.openUrl = openUrl;
        var snapshot = store.Current;
        ClientIdBox.Text = snapshot.Credentials?.ClientId ?? "";
        ClientSecretBox.Password = snapshot.Credentials?.ClientSecret ?? "";
        StatusText.Text = snapshot.Status switch
        {
            CredentialStatus.Ready => "인증값이 저장되어 있습니다. 변경 후 저장을 누르세요.",
            CredentialStatus.Unreadable => "저장한 인증값을 읽을 수 없습니다. 두 값을 다시 입력하고 저장하세요.",
            _ => "두 값을 입력한 뒤 저장하세요."
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            store.Save(ClientIdBox.Text, ClientSecretBox.Password);
            StatusText.Text = "저장했습니다. 다음 검색부터 적용됩니다. (API 인증 확인은 검색 시 수행됩니다.)";
        }
        catch (ArgumentException)
        {
            StatusText.Text = "두 인증값을 공백 없는 영문·숫자·기호로 입력하세요.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            StatusText.Text = "저장하지 못했습니다. 설정 폴더의 쓰기 권한과 Windows 계정을 확인하세요.";
        }
    }

    private void Help_Click(object sender, RoutedEventArgs e) => openUrl("https://developers.naver.com/docs/serviceapi/search/local/local.md");
}
