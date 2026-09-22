using System.Windows.Controls;
using Flow.Launcher.Plugin.NaverMapSearch.Search;
using Flow.Launcher.Plugin.NaverMapSearch.Settings;

namespace Flow.Launcher.Plugin.NaverMapSearch;

public sealed class Main : IAsyncPlugin, ISettingProvider, IDisposable
{
    private readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private PluginInitContext context = null!;
    private CredentialStore store = null!;
    private QueryService queries = null!;
    private ResultFactory results = null!;

    public Task InitAsync(PluginInitContext context)
    {
        this.context = context;
        store = new CredentialStore(Path.Combine(context.CurrentPluginMetadata.PluginSettingsDirectoryPath, "credentials.json"));
        queries = new QueryService(new NaverClient(http), store);
        results = new ResultFactory(url => context.API.OpenWebUrl(url), () => context.API.OpenSettingDialog());
        return Task.CompletedTask;
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var response = await queries.SearchAsync(query.Search, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return results.Create(response, query.ActionKeyword);
    }

    public Control CreateSettingPanel() => new SettingsPanel(store, url => context.API.OpenWebUrl(url));

    public void Dispose()
    {
        queries?.Dispose();
        http.Dispose();
    }
}
