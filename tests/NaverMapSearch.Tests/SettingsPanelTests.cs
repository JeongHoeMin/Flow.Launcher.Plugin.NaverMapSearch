using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin.NaverMapSearch.Settings;

namespace NaverMapSearch.Tests;

public sealed class SettingsPanelTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NaverMapSearch-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public Task Save_button_persists_values_and_new_panel_reloads_them() => OnSta(() =>
    {
        var path = Path.Combine(directory, "credentials.json");
        var store = new CredentialStore(path);
        var panel = new SettingsPanel(store, _ => { });
        var id = (TextBox)panel.FindName("ClientIdBox");
        var secret = (PasswordBox)panel.FindName("ClientSecretBox");
        id.Text = "panel-id";
        secret.Password = "panel-secret";
        ((Button)panel.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(CredentialStatus.Ready, new CredentialStore(path).Current.Status);
        var reopened = new SettingsPanel(new CredentialStore(path), _ => { });
        Assert.Equal("panel-id", ((TextBox)reopened.FindName("ClientIdBox")).Text);
        Assert.Equal("panel-secret", ((PasswordBox)reopened.FindName("ClientSecretBox")).Password);
        Assert.DoesNotContain("panel-secret", ((TextBlock)panel.FindName("StatusText")).Text);
        panel.Measure(new Size(580, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 580, panel.DesiredSize.Height));
        panel.UpdateLayout();
        Assert.True(panel.ActualHeight > 200);
    });

    [Fact]
    public Task Invalid_settings_show_feedback_without_saving() => OnSta(() =>
    {
        var store = new CredentialStore(Path.Combine(directory, "credentials.json"));
        var panel = new SettingsPanel(store, _ => { });
        ((Button)panel.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(CredentialStatus.Missing, store.Current.Status);
        Assert.Contains("입력", ((TextBlock)panel.FindName("StatusText")).Text);
    });

    [Fact]
    public Task Failed_save_has_feedback_and_preserves_previous_credentials() => OnSta(() =>
    {
        var path = Path.Combine(directory, "credentials.json");
        var store = new CredentialStore(path);
        store.Save("old-id", "old-secret");
        var panel = new SettingsPanel(store, _ => { });
        ((TextBox)panel.FindName("ClientIdBox")).Text = "new-id";
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        ((Button)panel.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("old-id", store.Current.Credentials!.ClientId);
        Assert.Contains("저장하지 못", ((TextBlock)panel.FindName("StatusText")).Text);
    });

    internal static Task OnSta(Action test)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { test(); completion.SetResult(); }
            catch (Exception ex) { completion.SetException(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
