using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.NaverMapSearch;
using Flow.Launcher.Plugin.NaverMapSearch.Search;

namespace NaverMapSearch.Tests;

public class ResultTests
{
    [Fact]
    public void Selecting_a_place_opens_its_map_search_and_closes_launcher()
    {
        string? opened = null;
        var factory = new ResultFactory(url => opened = url, () => throw new InvalidOperationException());
        var place = new PlacePresentation("가게", "카페 · 서울", "서울", "https://map.naver.com/p/search/example");
        var results = factory.Create(new(QueryStatus.Results, [place, place with { Title = "다음 가게" }]), "nm");
        Assert.Equal("가게", results[0].Title);
        Assert.Equal("카페 · 서울", results[0].SubTitle);
        Assert.True(results[0].Score > results[1].Score);
        Assert.True(results[0].Action(new ActionContext()));
        Assert.Equal("https://map.naver.com/p/search/example", opened);
        Assert.Equal("서울", results[0].CopyText);
    }

    [Theory]
    [InlineData(QueryStatus.MissingCredentials)]
    [InlineData(QueryStatus.UnreadableCredentials)]
    public void Credential_notice_can_open_settings(QueryStatus status)
    {
        var opened = false;
        var factory = new ResultFactory(_ => throw new InvalidOperationException(), () => opened = true);
        var result = Assert.Single(factory.Create(new(status, []), "nm"));
        Assert.True(result.Action(new ActionContext()));
        Assert.True(opened);
    }

    [Fact]
    public void Help_uses_users_configured_keyword()
    {
        var factory = new ResultFactory(_ => { }, () => { });
        var result = Assert.Single(factory.Create(new(QueryStatus.Help, []), "지도"));
        Assert.Contains("지도 강남 카페", result.SubTitle);
        Assert.False(result.Action(new ActionContext()));
    }

    [Theory]
    [InlineData(SearchError.Authentication, "인증")]
    [InlineData(SearchError.RateLimit, "한도")]
    [InlineData(SearchError.Network, "네트워크")]
    [InlineData(SearchError.Timeout, "시간")]
    [InlineData(SearchError.Server, "서버")]
    [InlineData(SearchError.InvalidResponse, "응답")]
    [InlineData(SearchError.Request, "요청")]
    public void Errors_have_actionable_korean_messages(SearchError error, string expected)
    {
        var factory = new ResultFactory(_ => { }, () => { });
        var result = Assert.Single(factory.Create(new(QueryStatus.Error, [], error), "nm"));
        Assert.Contains(expected, result.Title + result.SubTitle);
    }
}
