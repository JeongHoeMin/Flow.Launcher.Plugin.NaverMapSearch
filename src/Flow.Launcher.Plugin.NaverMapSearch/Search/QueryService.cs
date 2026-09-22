using Flow.Launcher.Plugin.NaverMapSearch.Settings;

namespace Flow.Launcher.Plugin.NaverMapSearch.Search;

public enum QueryStatus { Help, MissingCredentials, UnreadableCredentials, Results, Empty, Error }
public sealed record QueryResponse(QueryStatus Status, IReadOnlyList<PlacePresentation> Places, SearchError? Error = null);
public sealed class QueryService : IDisposable
{
    private readonly NaverClient client;
    private readonly CredentialStore store;
    private readonly object gate = new();
    private CancellationTokenSource? active;
    private bool disposed;

    public QueryService(NaverClient client, CredentialStore store)
    {
        this.client = client;
        this.store = store;
        store.Changed += CancelPending;
    }

    public async Task<QueryResponse> SearchAsync(string query, CancellationToken token)
    {
        CancellationTokenSource current;
        CredentialSnapshot snapshot;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            active?.Cancel();
            current = CancellationTokenSource.CreateLinkedTokenSource(token);
            active = current;
            snapshot = store.Current;
        }
        try
        {
            current.Token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(query)) return new(QueryStatus.Help, []);
            if (snapshot.Status != CredentialStatus.Ready)
                return new(snapshot.Status == CredentialStatus.Unreadable ? QueryStatus.UnreadableCredentials : QueryStatus.MissingCredentials, []);
            await Task.Delay(300, current.Token).ConfigureAwait(false);
            QueryResponse result;
            try
            {
                var places = await client.SearchAsync(query.Trim(), snapshot.Credentials!, current.Token).ConfigureAwait(false);
                result = new(places.Count == 0 ? QueryStatus.Empty : QueryStatus.Results, places.Select(PlacePresentation.From).ToArray());
            }
            catch (SearchException ex) { result = new(QueryStatus.Error, [], ex.Error); }
            // Some HTTP handlers complete even after cancellation; never return those stale results.
            current.Token.ThrowIfCancellationRequested();
            return result;
        }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(active, current)) active = null;
                current.Dispose();
            }
        }
    }

    private void CancelPending() { lock (gate) active?.Cancel(); }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            store.Changed -= CancelPending;
            active?.Cancel();
        }
    }
}
