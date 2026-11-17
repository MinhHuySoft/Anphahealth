# Báo cáo demo Anpha Health

**17/11/2026 · Blazor Web App .NET 10 · Chế độ: chuyển nội dung, dựng lại giao diện tham chiếu**

Đã dựng bản trình khách bằng Blazor Interactive Server, dùng nội dung Anpha và ngôn ngữ thiết kế Chemlabs. Bản demo có dữ liệu thật, các luồng duyệt chính và tài liệu phạm vi lập báo giá. Chưa cấu hình triển khai public.

## Độ phủ dữ liệu

Đã đối chiếu API WordPress công khai với HTML lưu: **15/15 page, 15/15 post, 124/124 trang media** đều có bản lưu. Ngoài ra có 23 URL taxonomy, tác giả, alias hoặc trang thành phần; tổng cộng **177 URL HTML thành công**. Có **127 tệp media** tải cục bộ, tổng 14.270.980 byte. Metadata 764 biến thể ảnh thumbnail được giữ trong JSON gốc; không tải lặp mọi thumbnail do WordPress sinh.

Kiểm kê dùng menu/liên kết nội bộ, robots.txt và REST API. `/sitemap.xml`, `/sitemap_index.xml`, `/wp-sitemap.xml` đều không có sitemap khả dụng. Đây là độ phủ nội dung công khai phát hiện được ngày khảo sát; không cam kết dữ liệu riêng tư, nội dung không liên kết hoặc dịch vụ bên ngoài. Video nhúng bên thứ ba chưa tải.

4 liên kết nguồn trả 404:

- `/tui-nang-nguc-impleo/chuyen-muc-khac`
- `/tin-tuyen-dung/tuyen-dung`
- `/chinh-sach-cong-ty/tuyen-dung`
- `/ung-tuyen/tuyen-dung`

Chi tiết: `RECON/coverage-audit.json`, `RECON/routes/anpha-route-map.json`, `data/asset-manifest.json`.

## Lựa chọn Chemlabs

Phát hiện 33 liên kết HTML: **29 thành công**, gồm 4 homepage và 25 trang con; 4 lỗi 404 (`shop-single.html`, `services.html`, `services-single.html`, `cart.html`). Chọn Home Supplies theo mô hình nhà phân phối, kết hợp bố cục giới thiệu doanh nghiệp của Home Main. Các trang kết quả xét nghiệm, đặt lịch xét nghiệm, đăng nhập nhà cung cấp, giỏ hàng/thanh toán không có nghiệp vụ tương ứng trong dữ liệu nguồn nên không được triển khai.

| Thành phần | Tham chiếu | Demo |
|---|---|---|
| Header, topbar, CTA | Home Main / Supplies | Logo và thông tin Anpha, menu tiếng Việt, liên hệ |
| Hero | Home Main left alignment, Supplies subject | Ảnh nguồn Anpha, navy overlay, heading Lexend, CTA |
| Danh mục | Supplies | 4 lĩnh vực, 8 mục gồm dòng Nagor, tìm kiếm/lọc |
| Chi tiết | Supplies single + single test sidebar | Nội dung crawl, tab hình ảnh, nhóm liên quan, tư vấn |
| Về công ty | About / Why Us | Lịch sử 2009–2014, tầm nhìn, sứ mệnh, văn hóa, giá trị |
| Tin tức | Blog / Blog single | 2 bài từ năm 2016, nội dung thật |
| Hoạt động, tuyển dụng | Research grid / Careers | Giữ trạng thái cập nhật của nguồn |
| Liên hệ | Contact | Địa chỉ, điện thoại/email, form kiểm tra và xem trước |

Đây là bản chuyển nội dung và tham chiếu thiết kế, không phải bản sao pixel. Mã CSS và component viết riêng; không dùng CSS/JS runtime hoặc ảnh stock preview của Chemlabs. Không dùng phép pixel diff để suy ra độ giống khi nội dung, ảnh, thương hiệu và cấu trúc nghiệp vụ đã thay đổi.

## Kiểm tra đã hoàn tất

- `dotnet build src/AnphaHealth.Web --no-restore`: **0 warning, 0 error**.
- `dotnet publish src/AnphaHealth.Web -c Release -o artifacts/publish --no-restore`: thành công.
- QA chạy trên **bản publish**, không chỉ bản development.
- **30 ảnh kiểm tra**: 10 tuyến đại diện × 3 viewport 1440, 768, 390 px.
- 1 H1 mỗi trang, không tràn ngang toàn trang, không ảnh lỗi, không hiện banner lỗi Blazor.
- 9 kiểm tra tương tác đạt: lọc danh mục, tìm kiếm, trạng thái không kết quả/reset, tab ảnh, tự chọn lĩnh vực từ trang sản phẩm, validation trường bắt buộc, xác nhận form cục bộ, sửa lại form, menu mobile điều hướng và đóng.
- **177/177 tuyến bản lưu** trả HTTP 200.
- Không có JavaScript page error, console error, response lỗi hoặc request ngoài localhost trên các lượt kiểm tra trang chính.

Bằng chứng máy đọc được: `RECON/clone-qa.json`. Script tái hiện: `scripts/browser-qa.mjs`.

Ảnh đối chiếu: `RECON/screenshots/anpha-original-*`, `chemlabs-*`, `clone-*`. Đã xem thủ công ảnh desktop toàn trang, đầu trang mobile, danh mục và liên hệ. Trong quá trình QA đã sửa tràn ngang slug dài ở trang kiểm kê và ảnh sản phẩm tràn che tab.

## Đánh giá theo mục tiêu demo

| Tiêu chí | Điểm / 5 | Căn cứ |
|---|---:|---|
| Bằng chứng nguồn | 5 | Raw HTML, API, đối chiếu toàn bộ bản ghi công khai |
| Cấu trúc | 4 | Đủ nhóm trang theo dữ liệu Anpha; rút các khối xét nghiệm không phù hợp |
| Hướng thị giác | 4 | Navy–teal, Lexend/Roboto, hero, sidebar/cards theo tham chiếu; ảnh và branding Anpha |
| Tương tác | 4 | Luồng duyệt, bộ lọc, tab, menu, validation; form dừng ở demo |
| Responsive | 5 | Ba kích thước đã kiểm tra và chụp ảnh, không tràn ngang |
| Nội dung chuyển đổi | 4 | Đủ bản lưu; nội dung cũ/placeholder cần khách duyệt |
| Chức năng production | 2 | Chưa có CMS, DB, gửi mail, đăng nhập hoặc quản lý dữ liệu thật |
| Quyền sử dụng khi xuất bản | 3 | Nguồn ghi rõ, CSS/component riêng, font có OFL; ảnh nguồn cần chủ sở hữu xác nhận |

Điểm là đánh giá nội bộ có căn cứ theo mục tiêu bản trình khách, không phải chứng nhận chất lượng hay điểm so khớp tự động.

## Các phần cần chốt để triển khai

Xem `docs/PHAM_VI_BAO_GIA.md`: quản trị nội dung/media, form thật và chống spam, tuyển dụng/CV, SEO/redirect, hosting, backup và bàn giao. Không tự đặt đơn giá hoặc thời gian cam kết khi chưa có yêu cầu nghiệp vụ được duyệt.
