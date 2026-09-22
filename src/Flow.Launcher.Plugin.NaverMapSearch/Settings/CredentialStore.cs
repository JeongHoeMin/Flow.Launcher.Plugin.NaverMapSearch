using Flow.Launcher.Plugin.NaverMapSearch.Search;
using System.Security.Cryptography;
using System.Text.Json;

namespace Flow.Launcher.Plugin.NaverMapSearch.Settings;

public enum CredentialStatus { Missing, Ready, Unreadable }
public sealed record CredentialSnapshot(CredentialStatus Status, Credentials? Credentials);
public sealed class CredentialStore
{
    private readonly string filePath;
    private readonly object gate = new();
    private CredentialSnapshot current;

    public CredentialStore(string filePath)
    {
        this.filePath = Path.GetFullPath(filePath);
        current = Load();
    }

    public CredentialSnapshot Current { get { lock (gate) return current; } }
    public event Action? Changed;

    public void Save(string id, string secret)
    {
        var credentials = new Credentials(id.Trim(), secret.Trim());
        Validate(credentials);
        lock (gate)
        {
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(credentials);
            byte[] encrypted;
            try { encrypted = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
            var document = new StoredCredentials(Convert.ToBase64String(encrypted));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var temporary = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonSerializer.Serialize(document));
                File.Move(temporary, filePath, overwrite: true);
                current = new(CredentialStatus.Ready, credentials);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        Changed?.Invoke();
    }

    private CredentialSnapshot Load()
    {
        try
        {
            var document = JsonSerializer.Deserialize<StoredCredentials>(File.ReadAllText(filePath));
            if (string.IsNullOrWhiteSpace(document?.ProtectedCredentials))
                return new(CredentialStatus.Unreadable, null);
            var plaintext = ProtectedData.Unprotect(Convert.FromBase64String(document.ProtectedCredentials), null, DataProtectionScope.CurrentUser);
            try
            {
                var credentials = JsonSerializer.Deserialize<Credentials>(plaintext);
                if (credentials is null) return new(CredentialStatus.Unreadable, null);
                Validate(credentials);
                return new(CredentialStatus.Ready, credentials);
            }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
        catch (FileNotFoundException) { return new(CredentialStatus.Missing, null); }
        catch (DirectoryNotFoundException) { return new(CredentialStatus.Missing, null); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException or ArgumentException)
        {
            return new(CredentialStatus.Unreadable, null);
        }
    }

    private static void Validate(Credentials credentials)
    {
        if (!ValidHeader(credentials.ClientId) || !ValidHeader(credentials.ClientSecret))
            throw new ArgumentException("Client ID와 Client Secret을 공백 없는 영문·숫자·기호로 입력하세요.");
    }

    private static bool ValidHeader(string? value) => !string.IsNullOrEmpty(value) && value.All(c => c is >= '!' and <= '~');
    private sealed record StoredCredentials(string ProtectedCredentials);
}
