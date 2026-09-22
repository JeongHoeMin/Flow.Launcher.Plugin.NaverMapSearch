using System.Reflection;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.NaverMapSearch;
using Flow.Launcher.Plugin.NaverMapSearch.Settings;

namespace NaverMapSearch.Tests;

public sealed class PluginTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NaverMapSearch-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Public_entrypoint_loads_and_returns_setup_action_without_credentials()
    {
        var api = DispatchProxy.Create<IPublicAPI, ApiRecorder>();
        var metadata = new PluginMetadata();
        typeof(PluginMetadata).GetProperty(nameof(PluginMetadata.PluginSettingsDirectoryPath))!.SetValue(metadata, directory);
        using var plugin = new Main();
        await plugin.InitAsync(new PluginInitContext(metadata, api));
        var query = new Query { ActionKeyword = "nm", SearchTerms = ["강남", "카페"] };
        // Flow's parser assigns Search separately through its internal setter.
        typeof(Query).GetProperty(nameof(Query.Search))!.SetValue(query, "강남 카페");
        var result = Assert.Single(await plugin.QueryAsync(query, default));
        Assert.Contains("인증값", result.Title);
        Assert.True(result.Action(new ActionContext()));
        Assert.Contains("OpenSettingDialog", ((ApiRecorder)(object)api).Calls);
        await SettingsPanelTests.OnSta(() => Assert.IsType<SettingsPanel>(plugin.CreateSettingPanel()));
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    public class ApiRecorder : DispatchProxy
    {
        public List<string> Calls { get; } = [];
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            return null;
        }
    }
}
