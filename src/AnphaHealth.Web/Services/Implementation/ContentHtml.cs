using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AnphaHealth.Web.Services.Models;

namespace AnphaHealth.Web.Services.Implementation;

internal static class ContentHtml
{
    private static readonly HashSet<string> Tags = new(StringComparer.Ordinal)
    {
        "p", "br", "hr", "h1", "h2", "h3", "h4", "h5", "h6", "div", "span", "section", "article",
        "strong", "b", "em", "i", "u", "s", "del", "sup", "sub", "small", "mark", "pre", "code",
        "blockquote", "ul", "ol", "li", "dl", "dt", "dd", "a", "img", "figure", "figcaption",
        "table", "caption", "thead", "tbody", "tfoot", "tr", "th", "td", "col", "colgroup"
    };

    private static readonly HashSet<string> Discard = new(StringComparer.Ordinal)
    {
        "script", "style", "iframe", "object", "embed", "template", "form", "input", "textarea",
        "button", "svg", "math", "link", "meta", "base"
    };

    public static (string Html, string Text, IReadOnlyList<string> ImagePaths) Sanitize(string html)
    {
        if (html.Length > 1_000_000)
            throw new CmsException("Nội dung vượt quá dung lượng cho phép. Hãy tải ảnh vào thư viện trước khi chèn.");

        var document = new HtmlParser().ParseDocument(html);
        var body = document.Body!;
        foreach (var element in body.QuerySelectorAll("*").ToArray())
        {
            if (Discard.Contains(element.LocalName))
            {
                element.Remove();
                continue;
            }

            if (!Tags.Contains(element.LocalName) || element.NamespaceUri != "http://www.w3.org/1999/xhtml")
            {
                element.Replace(element.ChildNodes.ToArray());
                continue;
            }

            foreach (var attribute in element.Attributes.ToArray())
            {
                if (!KeepAttribute(element.LocalName, attribute.LocalName, attribute.Value))
                    element.RemoveAttribute(attribute.Name);
            }

            if (element.LocalName == "a" && element.HasAttribute("href"))
                element.SetAttribute("rel", "noopener noreferrer");
        }

        var images = body.QuerySelectorAll("img[src]")
            .Select(image => image.GetAttribute("src")!)
            .Where(src => src.StartsWith("/media/", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return (body.InnerHtml, body.TextContent.Trim(), images);
    }

    private static bool KeepAttribute(string tag, string name, string value) => (tag, name) switch
    {
        ("a", "href") => IsLink(value),
        ("a", "target") => value == "_blank",
        ("img", "src") => IsMediaPath(value),
        ("img", "alt" or "title") or ("a", "title") => value.Length <= 500,
        ("img", "width" or "height") => int.TryParse(value, out var pixels) && pixels is >= 1 and <= 4000,
        ("td" or "th", "colspan" or "rowspan") => int.TryParse(value, out var span) && span is >= 1 and <= 100,
        ("ol", "start") => int.TryParse(value, out _),
        _ => false
    };


    private static bool IsMediaPath(string value)
    {
        if (!value.StartsWith("/media/", StringComparison.Ordinal))
            return false;

        var fileName = value.StartsWith("/media/uploads/", StringComparison.Ordinal) ? value[15..] : value[7..];
        return fileName.Length > 0 && fileName.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')
            && !fileName.Contains("..", StringComparison.Ordinal) && fileName.Contains('.');
    }

    private static bool IsLink(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || value.Contains('\\'))
            return false;

        return value[0] == '#' || (value[0] == '/' && !value.StartsWith("//", StringComparison.Ordinal)) ||
            (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" or "mailto" or "tel");
    }
}
