using System.Net;
using System.Text;
using Flow.Launcher.Plugin.NaverMapSearch.Search;

namespace NaverMapSearch.Tests;

public class SearchTests
{
    private static readonly Credentials Keys = new("test-id", "test-secret");
    private const string Response = """
        {"lastBuildDate":"Tue, 22 Sep 2026 12:00:00 +0900","total":1,"start":1,"display":1,"items":[
          {"title":"<b>강남</b> &amp; 카페","link":"https://example.com","category":"음식점>카페",
          "description":"","telephone":"","address":"서울 지번","roadAddress":"서울 도로 1","mapx":"1270000000","mapy":"370000000"}]}
        """;

    [Fact]
    public async Task Sends_encoded_query_and_per_request_headers_and_reads_places()
    {
        using var http = new HttpClient(new Handler((r, _) =>
        {
            Assert.Equal(HttpMethod.Get, r.Method);
            Assert.Equal("https://openapi.naver.com/v1/search/local.json?query=%EA%B0%95%EB%82%A8%20%26%20%EC%B9%B4%ED%8E%98&display=5&start=1&sort=random", r.RequestUri!.AbsoluteUri);
            Assert.Equal("test-id", Assert.Single(r.Headers.GetValues("X-Naver-Client-Id")));
            Assert.Equal("test-secret", Assert.Single(r.Headers.GetValues("X-Naver-Client-Secret")));
            return Task.FromResult(Json(Response));
        }));
        var places = await new NaverClient(http).SearchAsync("강남 & 카페", Keys, default);
        var place = Assert.Single(places);
        Assert.Equal("서울 도로 1", place.RoadAddress);
        Assert.Equal("<b>강남</b> &amp; 카페", place.Title);
        Assert.Empty(http.DefaultRequestHeaders);
    }

    [Theory]
    [InlineData(401, SearchError.Authentication)]
    [InlineData(403, SearchError.Authentication)]
    [InlineData(429, SearchError.RateLimit)]
    [InlineData(500, SearchError.Server)]
    [InlineData(400, SearchError.Request)]
    public async Task Classifies_http_errors_without_exposing_response(int status, SearchError expected)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("sensitive response") })));
        var error = await Assert.ThrowsAsync<SearchException>(() => new NaverClient(http).SearchAsync("a", Keys, default));
        Assert.Equal(expected, error.Error);
        Assert.DoesNotContain("sensitive", error.ToString());
    }

    [Fact]
    public async Task Empty_items_is_a_successful_empty_search()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{\"items\":[]}"))));
        Assert.Empty(await new NaverClient(http).SearchAsync("a", Keys, default));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"items\":null}")]
    [InlineData("{\"items\":[null]}")]
    public async Task Malformed_payload_is_reported(string body)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(body))));
        var error = await Assert.ThrowsAsync<SearchException>(() => new NaverClient(http).SearchAsync("a", Keys, default));
        Assert.Equal(SearchError.InvalidResponse, error.Error);
    }

    [Fact]
    public async Task Network_failure_is_classified()
    {
        using var http = new HttpClient(new Handler((_, _) => throw new HttpRequestException("private")));
        var error = await Assert.ThrowsAsync<SearchException>(() => new NaverClient(http).SearchAsync("a", Keys, default));
        Assert.Equal(SearchError.Network, error.Error);
        Assert.DoesNotContain("private", error.ToString());
    }

    [Fact]
    public async Task Http_timeout_is_distinct_from_caller_cancellation()
    {
        using var http = new HttpClient(new Handler(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return Json("{}"); })) { Timeout = TimeSpan.FromMilliseconds(50) };
        var error = await Assert.ThrowsAsync<SearchException>(() => new NaverClient(http).SearchAsync("a", Keys, default));
        Assert.Equal(SearchError.Timeout, error.Error);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new NaverClient(http).SearchAsync("a", Keys, cts.Token));
    }

    [Fact]
    public void Presentation_cleans_html_prefers_road_address_and_encodes_map_search()
    {
        var result = PlacePresentation.From(new Place("<b>강남</b> &amp; 카페", "음식점&gt;카페", "지번", "서울 도로 1"));
        Assert.Equal("강남 & 카페", result.Title);
        Assert.Equal("음식점>카페 · 서울 도로 1", result.Subtitle);
        Assert.Equal("서울 도로 1", result.Address);
        Assert.Equal("https://map.naver.com/p/search/%EC%84%9C%EC%9A%B8%20%EB%8F%84%EB%A1%9C%201%20%EA%B0%95%EB%82%A8%20%26%20%EC%B9%B4%ED%8E%98", result.MapUrl);
    }

    [Fact]
    public void Presentation_falls_back_to_street_address()
    {
        var result = PlacePresentation.From(new Place("가게", "", "서울 지번", "  "));
        Assert.Equal("서울 지번", result.Address);
        Assert.Equal("서울 지번", result.Subtitle);
    }

    internal static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    internal sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
