using System.Net;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.NaverMapSearch.Search;

public sealed record PlacePresentation(string Title, string Subtitle, string Address, string MapUrl)
{
    public static PlacePresentation From(Place place)
    {
        var title = Clean(place.Title);
        var category = Clean(place.Category);
        var address = Clean(string.IsNullOrWhiteSpace(place.RoadAddress) ? place.Address : place.RoadAddress);
        var subtitle = string.Join(" · ", new[] { category, address }.Where(x => x.Length > 0));
        var search = string.Join(" ", new[] { address, title }.Where(x => x.Length > 0));
        return new(title, subtitle, address, "https://map.naver.com/p/search/" + Uri.EscapeDataString(search));
    }

    private static string Clean(string value) => WebUtility.HtmlDecode(
        Regex.Replace(value, "<[^>]*>", "", RegexOptions.NonBacktracking)).Trim();
}
