# Hướng dẫn thay nội dung

- **Logo/ảnh:** `src/AnphaHealth.Web/wwwroot/media`. Dùng `ContentStore.Asset("tên-file-gốc")` để tra filename có hash; nguồn từng tệp ở `data/asset-manifest.json`.
- **Sản phẩm/danh mục:** danh sách `Products` trong `src/AnphaHealth.Web/Services/ContentStore.cs` điều khiển nhóm ngành, thẻ, bộ lọc, thương hiệu và ảnh đại diện.
- **Nội dung dài:** `data/anpha-content.json`, tra theo `slug`. File được copy vào thư mục output khi build/publish; thay dữ liệu rồi build lại.
- **Trang chủ:** `Components/Pages/Home.razor`. **Giới thiệu:** `About.razor`. **Tin tức:** `Shared/NewsCards.razor` + nội dung theo slug.
- **Liên hệ:** `Contact.razor` và `Layout/MainLayout.razor`. Form hiện chỉ giữ dữ liệu trong phiên trình duyệt/Blazor circuit, không có dịch vụ gửi email hoặc ghi DB.
- **Màu/font/khoảng cách:** `wwwroot/site.css`; các token nằm ở `:root`. Font self-hosted trong `wwwroot/fonts`.
- **Bố cục chung:** `Layout/MainLayout.razor`, `Shared/PageHero.razor`, `ProductCard.razor`, `Icon.razor`.
- **Crawl:** chạy `scripts/crawl.py` sẽ tải lại dữ liệu công khai và ghi đè snapshot/manifest. Chỉ chạy khi có nhu cầu cập nhật; cần đối chiếu nội dung khách đã duyệt trước khi thay.
- **Báo giá:** `docs/PHAM_VI_BAO_GIA.md` và trang `/de-xuat`. Đây là hồ sơ phạm vi; chưa có đơn giá được doanh nghiệp duyệt.

Luồng chính: Trang chủ → Danh mục → Chi tiết → Liên hệ. Các URL cũ trong nội dung dài dẫn vào `/noi-dung/{slug}` để đọc bản lưu. Triển khai production cần bảng redirect 301 từng URL cũ sang URL mới và bỏ trang lưu không cần thiết khỏi index tìm kiếm.
