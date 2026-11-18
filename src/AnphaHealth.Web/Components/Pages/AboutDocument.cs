using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace AnphaHealth.Web.Components.Pages;

internal sealed record AboutSection(string Title, string Html);

internal sealed record AboutDocument(string Introduction, IReadOnlyList<AboutSection> Sections,
    bool HasFoundingYear, bool HasExpansionYear)
{
    public const string DefaultCover = "bigstock-Medicine-Doctor-Hand-Working-W-44541469_opt.jpg";
    private const string LegacyCoverPath = "/media/826f6f4bd5bd-" + DefaultCover;
    private const string LegacyCatalogTitle = "Các ngành hàng chủ đạo của công ty:";

    // Imported WordPress content includes a product table. The current catalog supplies the public links.
    private static readonly string[] LegacyCatalogImages =
    [
        "/media/f22d532e91f2-image11.jpg",
        "/media/33f468aee305-image12.jpg",
        "/media/031c09edb3e6-image1.jpg",
        "/media/5ae801c7ac78-6033_196904493797345_874716314_n.png"
    ];

    private static readonly string[] LegacyCatalogLabels =
    [
        "Vật tư lọc máu – lọc thận nhân tạo",
        "Vật tư cho máy Tim phổi – Van tim",
        "Vật tư can thiệp mạch vành, mạch máu ngoại biên",
        "Vât liệu cấy ghép ngực"
    ];

    public static AboutDocument Read(string html, bool showCatalog)
    {
        var root = new HtmlParser().ParseDocument(html).Body!;
        IElement body = root.Children.Length == 1 && root.FirstElementChild is { } wrapper
            && wrapper.ClassList.Contains("story") ? wrapper : root;

        var nodes = body.ChildNodes.Where(HasVisibleContent).ToList();
        if (nodes.FirstOrDefault() is IElement first && IsLegacyCover(first))
            nodes.RemoveAt(0);

        if (showCatalog)
            RemoveLegacyCatalog(nodes);

        var introduction = new StringBuilder();
        var sectionContent = introduction;
        var sections = new List<AboutSection>();
        string? heading = null;

        foreach (var node in nodes)
        {
            if (node is IElement { LocalName: "h1" or "h2" or "h3" or "h4" } title)
            {
                if (heading is not null)
                    sections.Add(new AboutSection(heading, sectionContent.ToString()));

                heading = title.TextContent.Trim();
                sectionContent = new StringBuilder();
                continue;
            }

            sectionContent.Append(node.ToHtml());
        }

        if (heading is not null)
            sections.Add(new AboutSection(heading, sectionContent.ToString()));

        var text = body.TextContent;
        return new AboutDocument(introduction.ToString(), sections,
            text.Contains("năm 2009", StringComparison.OrdinalIgnoreCase),
            text.Contains("năm 2014", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasVisibleContent(INode node) => node switch
    {
        IElement element => !string.IsNullOrWhiteSpace(element.TextContent)
            || element.LocalName is "img" or "hr" or "table"
            || element.QuerySelector("img, hr, table") is not null,
        IText text => !string.IsNullOrWhiteSpace(text.Data),
        _ => false
    };

    private static bool IsLegacyCover(IElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.TextContent))
            return false;

        var image = element.LocalName == "img" ? element : element.QuerySelector("img:only-child");
        return image?.GetAttribute("src") == LegacyCoverPath;
    }

    private static void RemoveLegacyCatalog(List<INode> nodes)
    {
        for (var index = 0; index < nodes.Count - 1; index++)
        {
            if (nodes[index].TextContent.Trim() != LegacyCatalogTitle ||
                nodes[index + 1] is not IElement { LocalName: "table" } table)
                continue;

            var images = table.QuerySelectorAll("img").Select(image => image.GetAttribute("src"));
            var labels = table.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim());
            if (images.SequenceEqual(LegacyCatalogImages) && labels.SequenceEqual(LegacyCatalogLabels))
                nodes.RemoveRange(index, 2);
            return;
        }
    }
}
