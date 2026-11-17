# Đề xuất làm mới website Anpha Health

**Ngày khảo sát: 17/11/2026 · Công nghệ: Blazor Web App (.NET 10)**

Mục tiêu là xây dựng website doanh nghiệp dành cho nhà nhập khẩu và phân phối thiết bị y tế: giới thiệu năng lực, trình bày danh mục theo chuyên ngành, cung cấp thông tin sản phẩm và tiếp nhận nhu cầu hợp tác. Nội dung lấy từ website Anpha hiện tại; hướng giao diện dựa trên Chemlabs, điều chỉnh về thương hiệu Anpha.

## Căn cứ dữ liệu

| Nguồn | Kết quả |
|---|---|
| WordPress pages công khai | 15 bản ghi, gồm cả các khối trang chủ/footer |
| WordPress posts công khai | 15 bản ghi, gồm sản phẩm, bài viết và mục đang cập nhật |
| WordPress media công khai | 124 bản ghi, giữ cả metadata các kích thước ảnh |
| Toàn bộ URL HTML tìm được và lưu thành công | 177 URL, không tương đương 177 giao diện riêng |
| Media gốc/ảnh đang dùng tải thành công | 127 tệp, có manifest nguồn |
| Liên kết Anpha lỗi | 4 URL trả 404; xem báo cáo crawl |
| Chemlabs | 29 trang HTML truy cập được: 4 homepage + 25 trang con; 4 liên kết 404 |

Không tìm thấy sitemap khả dụng tại các đường dẫn chuẩn đã kiểm tra. Độ phủ được đối chiếu với API WordPress pages/posts/media và liên kết nội bộ. Phạm vi là toàn bộ nội dung **công khai tìm được tại thời điểm khảo sát**, không bao gồm quản trị, bản nháp, nội dung bị ẩn và tài nguyên bên thứ ba chưa tải như video nhúng.

## Giao diện được chọn

| Hạng mục | Chemlabs tham chiếu | Nội dung Anpha | Kết quả demo |
|---|---|---|---|
| Trang chủ | `home-supllies.html`, `index.html` | Công ty, 4 chuyên ngành, hãng, sản phẩm, tin tức | Hero, khối chuyên ngành, logo hãng, giới thiệu, danh mục, tin tức, liên hệ |
| Giới thiệu | `about-us.html`, `why-us.html` | Thành lập 2009, mở rộng 2014, tầm nhìn, sứ mệnh, văn hóa, giá trị | Trang doanh nghiệp có nội dung thật |
| Danh mục sản phẩm | `supplies.html` | FMC, CID, Medtronic, Nagor | 8 mục gồm 4 nhóm chính + 4 dòng Nagor; tìm kiếm và lọc |
| Chi tiết sản phẩm | `supplies-single.html`, `tests-single-test.html` | Thông tin hãng, mô tả, ảnh, bảng thông tin gốc | Mẫu dùng chung cho 8 mục; tab thông tin/ảnh; liên hệ theo lĩnh vực |
| Tin tức | `blog.html` | 2 bài viết hiện có | Danh sách giữ nguyên ngày đăng |
| Chi tiết tin | `blog-single-post.html` | Nội dung bài viết gốc | Trang đọc bài; video nhúng ngoài chỉ ghi nhận trong bản lưu |
| Hoạt động | `research-grid.html` | Hình ảnh công ty, hoạt động xã hội | Khung hiển thị; giữ trạng thái đang cập nhật |
| Tuyển dụng | `careers.html` | Tin tuyển dụng, chính sách, ứng tuyển | Khung 3 mục; giữ trạng thái đang cập nhật |
| Liên hệ | `contact-us.html` | Trụ sở, văn phòng, điện thoại, email | Thông tin công ty + form có kiểm tra dữ liệu, xác nhận tại demo |

Tổng cộng **8 nhóm trang chính**, trong đó tin tức có 2 mẫu danh sách/chi tiết. Dùng chung thành phần giao diện để triển khai nhất quán trên desktop, tablet và điện thoại. Trang nội bộ `/de-xuat` là tài liệu duyệt demo; kho `/noi-dung/...` dùng đối chiếu dữ liệu cũ.

Home Supplies phù hợp với mô hình nhà phân phối. Các mẫu xét nghiệm, đặt lịch lấy mẫu, tra kết quả, đăng nhập nhà cung cấp, giỏ hàng và thanh toán của Chemlabs không có nghiệp vụ tương ứng trong dữ liệu Anpha, nên chưa đưa vào phạm vi.

## Phạm vi đề nghị báo giá triển khai chính thức

| Hạng mục | Đầu ra cần nghiệm thu | Tình trạng hiện tại |
|---|---|---|
| Hoàn thiện giao diện | Duyệt màu, font, ảnh, nội dung, các breakpoint, các trạng thái tương tác | Có bản demo |
| Di chuyển nội dung | Chuyển page/post cần giữ, loại khối WordPress cũ, chuẩn hóa danh mục, rà ảnh | Có dữ liệu crawl; cần khách duyệt |
| Quản trị nội dung | Đăng nhập, phân quyền, quản lý sản phẩm/danh mục/nhãn hàng, tin tức, tuyển dụng, hoạt động, thông tin liên hệ | Chưa xây dựng |
| Quản lý media | Upload, chọn ảnh, tối ưu kích thước, mô tả ảnh, kiểm tra định dạng/dung lượng | Demo dùng ảnh tĩnh |
| Tiếp nhận liên hệ | Lưu yêu cầu, email thông báo, kiểm tra dữ liệu, chống spam, chính sách lưu dữ liệu | Demo chỉ xem trước, không gửi/lưu |
| Tuyển dụng | CMS bài tuyển dụng; biểu mẫu ứng tuyển/CV nếu khách yêu cầu | Chưa có dữ liệu vị trí thực |
| SEO và chuyển đổi URL | Metadata từng trang, canonical, sitemap, robots, redirect 301, kiểm kê URL cũ | Cần thực hiện khi triển khai |
| Hạ tầng | Hosting phù hợp ASP.NET Core/Blazor, tên miền, HTTPS, giám sát, log, backup, quy trình cập nhật | Chạy cục bộ |
| Kiểm thử & bàn giao | Kiểm thử đa thiết bị, dữ liệu, form thực, bảo mật, tài liệu quản trị, hướng dẫn vận hành | Có kiểm tra UI; hệ thống production cần kiểm thử riêng |

Đơn giá, tổng giá, thời gian triển khai, bảo hành và chi phí vận hành được chốt sau khi thống nhất các hạng mục trên. Tài liệu này là **phạm vi kỹ thuật kèm bản trình giao diện**, chưa phải báo giá tài chính có hiệu lực.

## Nội dung cần khách duyệt

1. Địa chỉ và thông tin liên hệ đang giữ nguyên cách ghi trên website cũ.
2. Quan hệ phân phối với từng hãng, danh mục sản phẩm còn kinh doanh, mô tả và thông số hiện hành.
3. Các mức giá lịch sử trong nội dung gốc từ năm 2016; chưa dùng làm giá kinh doanh hiện tại.
4. Hình ảnh công ty, hoạt động xã hội, tuyển dụng, chính sách và ứng tuyển: nguồn chỉ ghi “Đang cập nhật”.
5. Quyền sử dụng logo, hình ảnh và bài viết khi xuất bản. Ảnh stock trên nguồn chưa có hồ sơ license trong dữ liệu crawl.
6. Nếu yêu cầu dùng nguyên file thương mại Chemlabs, cần license của 7oroof. Demo hiện viết component/CSS riêng; ảnh preview của Chemlabs không được tái dùng.

## Tài liệu đối chiếu

- Anpha: https://anphahealth.vn/
- Chemlabs: https://7oroof.com/demos/chemlabs/index.html
- Chemlabs Home Supplies: https://7oroof.com/demos/chemlabs/home-supllies.html
- Sản phẩm template chính thức: https://themeforest.net/item/chemlabs-laboratory-science-research-html5-template/49210595
- Kiểm kê: `RECON/routes/anpha-route-map.json`, `RECON/routes/chemlabs-route-map.json`.
- Nội dung: `data/anpha-content.json`; media: `data/asset-manifest.json`.
