namespace AnphaHealth.Web.Services;

public static class PublicPageRoutes
{
    public static bool IsSystemPage(string slug) => slug is
        "home" or "gioi-thieu" or "san-pham-page" or "tin-tuc" or "lien-he"
        or "hinh-anh-cong-ty" or "hoat-dong-xa-hoi" or "tin-tuyen-dung" or "chinh-sach-cong-ty" or "ung-tuyen";
}
