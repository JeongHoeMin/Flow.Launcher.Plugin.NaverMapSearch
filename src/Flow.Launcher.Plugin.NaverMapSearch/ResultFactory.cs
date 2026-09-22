using Flow.Launcher.Plugin.NaverMapSearch.Search;

namespace Flow.Launcher.Plugin.NaverMapSearch;

internal sealed class ResultFactory(Action<string> openUrl, Action openSettings)
{
    public List<Result> Create(QueryResponse response, string keyword)
    {
        if (response.Status == QueryStatus.Results)
            return response.Places.Select((place, index) => new Result
            {
                Title = place.Title,
                SubTitle = place.Subtitle,
                TitleToolTip = place.Title,
                SubTitleToolTip = place.Subtitle,
                CopyText = place.Address,
                IcoPath = "Images/icon.png",
                Score = 100 - index,
                Action = _ => { openUrl(place.MapUrl); return true; }
            }).ToList();

        return [response.Status switch
        {
            QueryStatus.Help => Notice("네이버 지역 검색", $"예: {keyword} 강남 카페 · 최대 5개 장소를 검색합니다."),
            QueryStatus.MissingCredentials => Notice("네이버 API 인증값을 설정하세요", "Enter → 설정 → 플러그인 → NaverMapSearch에서 Client ID와 Secret을 입력하세요.", true),
            QueryStatus.UnreadableCredentials => Notice("저장한 인증값을 읽을 수 없습니다", "Windows 계정 변경 또는 파일 손상: 설정에서 인증값을 다시 저장하세요.", true),
            QueryStatus.Empty => Notice("검색 결과가 없습니다", "지역명과 장소명을 함께 입력해 보세요. 예: 강남 카페"),
            _ => Error(response.Error)
        }];
    }

    private Result Error(SearchError? error) => error switch
    {
        SearchError.Authentication => Notice("네이버 API 인증 또는 권한 오류", "설정의 인증값과 네이버 개발자 센터의 검색 API 사용 권한을 확인하세요.", true),
        SearchError.RateLimit => Notice("네이버 API 호출 한도에 도달했습니다", "잠시 후 다시 시도하거나 네이버 개발자 센터에서 사용량을 확인하세요."),
        SearchError.Network => Notice("네트워크에 연결할 수 없습니다", "인터넷 연결을 확인한 뒤 다시 검색하세요."),
        SearchError.Timeout => Notice("검색 응답 시간이 초과되었습니다", "잠시 후 다시 검색하세요."),
        SearchError.Server => Notice("네이버 검색 서버 오류", "잠시 후 다시 검색하세요."),
        SearchError.Request => Notice("검색 요청을 처리할 수 없습니다", "검색어를 변경한 뒤 다시 검색하세요."),
        _ => Notice("네이버 검색 응답을 읽을 수 없습니다", "잠시 후 다시 검색하세요. 문제가 계속되면 플러그인 업데이트를 확인하세요.")
    };

    private Result Notice(string title, string subtitle, bool settings = false) => new()
    {
        Title = title,
        SubTitle = subtitle,
        IcoPath = "Images/icon.png",
        Action = _ => { if (settings) openSettings(); return settings; }
    };
}
