using Flow.Launcher.Plugin.NaverMapSearch.Search;
using Flow.Launcher.Plugin.NaverMapSearch.Settings;

namespace NaverMapSearch.Tests;

public sealed class QueryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NaverMapSearch-tests", Guid.NewGuid().ToString("N"));
    private CredentialStore Store(bool ready = true)
    {
        var store = new CredentialStore(Path.Combine(directory, "credentials.json"));
        if (ready) store.Save("id", "secret");
        return store;
    }

    [Fact]
    public async Task Blank_query_and_missing_credentials_do_not_call_api()
    {
        using var http = new HttpClient(new SearchTests.Handler((_, _) => throw new InvalidOperationException("Unexpected HTTP request")));
        using var queries = new QueryService(new NaverClient(http), Store(false));
        Assert.Equal(QueryStatus.Help, (await queries.SearchAsync("  ", default)).Status);
        Assert.Equal(QueryStatus.MissingCredentials, (await queries.SearchAsync("카페", default)).Status);
    }

    [Fact]
    public async Task Corrupt_credentials_are_explained_without_http()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "credentials.json"), "broken");
        using var http = new HttpClient(new SearchTests.Handler((_, _) => throw new InvalidOperationException("Unexpected HTTP request")));
        using var queries = new QueryService(new NaverClient(http), Store(false));
        Assert.Equal(QueryStatus.UnreadableCredentials, (await queries.SearchAsync("카페", default)).Status);
    }

    [Fact]
    public async Task Rapid_input_only_sends_latest_query_after_debounce()
    {
        var requests = new List<string>();
        using var http = new HttpClient(new SearchTests.Handler((r, _) => { requests.Add(r.RequestUri!.Query); return Task.FromResult(SearchTests.Json("{\"items\":[]}")); }));
        using var queries = new QueryService(new NaverClient(http), Store());
        var old = queries.SearchAsync("강", default);
        var latest = queries.SearchAsync("강남", default);
        Assert.Empty(requests);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => old);
        Assert.Equal(QueryStatus.Empty, (await latest).Status);
        Assert.Contains("%EA%B0%95%EB%82%A8", Assert.Single(requests));
    }

    [Fact]
    public async Task Late_response_cannot_replace_newer_results()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new SearchTests.Handler(async (r, _) =>
        {
            if (r.RequestUri!.Query.Contains("old")) { started.SetResult(); await finish.Task; }
            return SearchTests.Json("{\"items\":[{\"title\":\"카페\",\"category\":\"카페\",\"address\":\"서울\",\"roadAddress\":\"\"}]}");
        }));
        using var queries = new QueryService(new NaverClient(http), Store());
        var old = queries.SearchAsync("old", default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var latest = await queries.SearchAsync("new", default);
        finish.SetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => old);
        Assert.Equal("카페", Assert.Single(latest.Places).Title);
    }

    [Fact]
    public async Task Credential_change_cancels_pending_work_and_next_request_uses_new_values()
    {
        var store = Store();
        using var http = new HttpClient(new SearchTests.Handler((r, _) =>
        {
            Assert.Equal("new-id", Assert.Single(r.Headers.GetValues("X-Naver-Client-Id")));
            Assert.Equal("new-secret", Assert.Single(r.Headers.GetValues("X-Naver-Client-Secret")));
            return Task.FromResult(SearchTests.Json("{\"items\":[]}"));
        }));
        using var queries = new QueryService(new NaverClient(http), store);
        var old = queries.SearchAsync("old", default);
        store.Save("new-id", "new-secret");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => old);
        Assert.Equal(QueryStatus.Empty, (await queries.SearchAsync("new", default)).Status);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_presented_as_an_error()
    {
        using var http = new HttpClient(new SearchTests.Handler((_, _) => throw new InvalidOperationException("Unexpected HTTP request")));
        using var queries = new QueryService(new NaverClient(http), Store());
        using var cts = new CancellationTokenSource();
        var request = queries.SearchAsync("카페", cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
    }

    [Fact]
    public async Task Service_turns_api_failure_into_displayable_status()
    {
        using var http = new HttpClient(new SearchTests.Handler((_, _) => throw new HttpRequestException()));
        using var queries = new QueryService(new NaverClient(http), Store());
        var result = await queries.SearchAsync("카페", default);
        Assert.Equal(QueryStatus.Error, result.Status);
        Assert.Equal(SearchError.Network, result.Error);
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
