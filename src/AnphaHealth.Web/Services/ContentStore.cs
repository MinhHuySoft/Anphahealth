using System.Text.Json;
namespace AnphaHealth.Web.Services;
public sealed class ContentPage
{
    public string Url { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Html { get; set; } = "";
    public string Text { get; set; } = "";
    public string[] LocalImages { get; set; } = [];
}
public sealed record Product(string Slug, string Brand, string Category, string Title, string Description, string Image, string Icon, bool Primary = true);
public sealed class ContentStore
{
    public IReadOnlyList<ContentPage> Pages { get; }
    private readonly string[] media;
    public ContentStore(IWebHostEnvironment env)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "anpha-content.json"));
        Pages = JsonSerializer.Deserialize<List<ContentPage>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        media = Directory.GetFiles(Path.Combine(env.WebRootPath, "media")).Select(Path.GetFileName).OfType<string>().ToArray();
    }
    public ContentPage? Get(string slug) => Pages.FirstOrDefault(x => x.Slug == slug.Trim('/'));
    public string Asset(string filename) => "/media/" + (media.FirstOrDefault(x => x.EndsWith("-" + filename, StringComparison.OrdinalIgnoreCase)) ?? filename);
    public IReadOnlyList<Product> Products => [
        new("fresenious-medicalcare-fmc", "Fresenius Medical Care", "Lọc máu & lọc thận", "Lọc máu, lọc thận nhân tạo", "Máy lọc thận, màng lọc và vật tư cho điều trị thay thế thận.", Asset("image21.jpg"), "pulse"),
        new("c-i-d", "CID · Alvimedica", "Can thiệp mạch", "Can thiệp mạch vành & ngoại biên", "Stent, bóng nong và ống thông phục vụ can thiệp mạch.", Asset("image3.jpg"), "heart"),
        new("medtronic", "Medtronic", "Tim phổi & van tim", "Tim phổi, van tim", "Van tim và các vật tư cho phẫu thuật tim, tuần hoàn ngoài cơ thể.", Asset("image22.jpg"), "cross"),
        new("nagor-page", "Nagor", "Cấy ghép ngực", "Vật liệu cấy ghép ngực", "Các dòng sản phẩm cấy ghép ngực CoGel, IMPLEO, RGI và GFX.", Asset("Picture15.jpg"), "shield"),
        new("cogel", "Nagor", "Cấy ghép ngực", "CoGel", "Dòng sản phẩm cấy ghép ngực CoGel của Nagor.", Asset("cogel_Nagor.jpg"), "shield", false),
        new("tui-nang-nguc-impleo", "Nagor", "Cấy ghép ngực", "IMPLEO", "Thông tin dòng túi nâng ngực IMPLEO từ website hiện tại.", Asset("impleo_impleo.jpg"), "shield", false),
        new("rgi", "Nagor", "Cấy ghép ngực", "RGI", "Dòng sản phẩm cấy ghép ngực RGI của Nagor.", Asset("RGI_nagor.jpg"), "shield", false),
        new("gfx", "Nagor", "Cấy ghép ngực", "GFX", "Dòng sản phẩm cấy ghép ngực GFX của Nagor.", Asset("GFX_nagor.jpg"), "shield", false)
    ];
    public string Route(string slug) => slug switch
    {
        "home" => "/", "gioi-thieu" => "/gioi-thieu", "san-pham-page" => "/san-pham", "tin-tuc" => "/tin-tuc", "lien-he" => "/lien-he",
        _ when Products.Any(p => p.Slug == slug) => "/san-pham/" + slug,
        "tim-hieu-ve-tui-nang-nguc" or "gioi-thieu-ve-tap-doan-gca-nagor" => "/tin-tuc/" + slug,
        _ => "/noi-dung/" + slug
    };
}
