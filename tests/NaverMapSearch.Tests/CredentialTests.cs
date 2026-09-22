using Flow.Launcher.Plugin.NaverMapSearch.Search;
using Flow.Launcher.Plugin.NaverMapSearch.Settings;

namespace NaverMapSearch.Tests;

public sealed class CredentialTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NaverMapSearch-tests", Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(directory, "credentials.json");

    [Fact]
    public void Missing_file_prompts_for_settings()
    {
        Assert.Equal(CredentialStatus.Missing, new CredentialStore(FilePath).Current.Status);
    }

    [Fact]
    public void Save_encrypts_both_values_and_reload_recovers_them()
    {
        var store = new CredentialStore(FilePath);
        store.Save(" sample-client-id ", " sample-client-secret ");
        var json = File.ReadAllText(FilePath);
        Assert.DoesNotContain("sample-client-id", json);
        Assert.DoesNotContain("sample-client-secret", json);
        var snapshot = new CredentialStore(FilePath).Current;
        Assert.Equal(CredentialStatus.Ready, snapshot.Status);
        Assert.Equal(new Credentials("sample-client-id", "sample-client-secret"), snapshot.Credentials);
    }

    [Fact]
    public void Update_is_immediate_and_persisted_and_notifies_search()
    {
        var store = new CredentialStore(FilePath);
        store.Save("old-id", "old-secret");
        var changes = 0;
        store.Changed += () => changes++;
        store.Save("new-id", "new-secret");
        Assert.Equal("new-id", store.Current.Credentials!.ClientId);
        Assert.Equal("new-secret", new CredentialStore(FilePath).Current.Credentials!.ClientSecret);
        Assert.Equal(1, changes);
    }

    [Theory]
    [InlineData("broken json")]
    [InlineData("{\"ProtectedCredentials\":\"not-base64!\"}")]
    [InlineData("{\"ProtectedCredentials\":\"YWJj\"}")]
    [InlineData("{}")]
    public void Corrupt_file_requires_reentry_and_can_be_replaced(string json)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, json);
        var store = new CredentialStore(FilePath);
        Assert.Equal(CredentialStatus.Unreadable, store.Current.Status);
        Assert.Null(store.Current.Credentials);
        store.Save("replacement-id", "replacement-secret");
        Assert.Equal(CredentialStatus.Ready, new CredentialStore(FilePath).Current.Status);
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("id", " ")]
    [InlineData("bad\r\nid", "secret")]
    [InlineData("id", "한글")]
    public void Invalid_header_values_do_not_replace_saved_credentials(string id, string secret)
    {
        var store = new CredentialStore(FilePath);
        store.Save("good-id", "good-secret");
        Assert.Throws<ArgumentException>(() => store.Save(id, secret));
        Assert.Equal("good-id", new CredentialStore(FilePath).Current.Credentials!.ClientId);
    }

    [Fact]
    public void Failed_write_does_not_change_in_memory_credentials()
    {
        var store = new CredentialStore(FilePath);
        store.Save("good-id", "good-secret");
        using var locked = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var error = Record.Exception(() => store.Save("bad-id", "bad-secret"));
        Assert.True(error is IOException or UnauthorizedAccessException);
        Assert.Equal("good-id", store.Current.Credentials!.ClientId);
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
