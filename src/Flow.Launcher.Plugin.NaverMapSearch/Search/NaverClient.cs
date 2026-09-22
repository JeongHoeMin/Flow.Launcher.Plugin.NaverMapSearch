using System.Net;
using System.Text.Json;

namespace Flow.Launcher.Plugin.NaverMapSearch.Search;

public sealed record Credentials(string ClientId, string ClientSecret);
public sealed record Place(string Title, string Category, string Address, string RoadAddress);
public enum SearchError { Authentication, RateLimit, Network, Timeout, InvalidResponse, Server, Request }
public sealed class SearchException(SearchError error) : Exception
{
    public SearchError Error { get; } = error;
}
public sealed class NaverClient(HttpClient http)
{
    public async Task<IReadOnlyList<Place>> SearchAsync(string query, Credentials credentials, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                "https://openapi.naver.com/v1/search/local.json?query=" + Uri.EscapeDataString(query) + "&display=5&start=1&sort=random");
            request.Headers.Add("X-Naver-Client-Id", credentials.ClientId);
            request.Headers.Add("X-Naver-Client-Secret", credentials.ClientSecret);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new SearchException(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => SearchError.Authentication,
                    HttpStatusCode.TooManyRequests => SearchError.RateLimit,
                    >= HttpStatusCode.InternalServerError => SearchError.Server,
                    _ => SearchError.Request
                });

            using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
            if (json.RootElement.ValueKind != JsonValueKind.Object ||
                !json.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                throw new SearchException(SearchError.InvalidResponse);
            var places = new List<Place>();
            foreach (var item in items.EnumerateArray().Take(5))
            {
                if (item.ValueKind != JsonValueKind.Object)
                    throw new SearchException(SearchError.InvalidResponse);
                places.Add(new Place(Read(item, "title"), Read(item, "category"), Read(item, "address"), Read(item, "roadAddress")));
            }
            timeout.Token.ThrowIfCancellationRequested();
            return places;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new SearchException(SearchError.Timeout);
        }
        catch (HttpRequestException) { throw new SearchException(SearchError.Network); }
        catch (IOException) { throw new SearchException(SearchError.Network); }
        catch (JsonException) { throw new SearchException(SearchError.InvalidResponse); }
    }

    private static string Read(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return "";
        if (value.ValueKind != JsonValueKind.String) throw new SearchException(SearchError.InvalidResponse);
        return value.GetString() ?? "";
    }
}
