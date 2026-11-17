# Kiểm kê trước bàn giao demo

- Bản publish cục bộ đã build và được QA trình duyệt ở 1440/768/390 px.
- Nội dung Anpha giữ trong JSON; runtime không dùng HTML head, CSS, JS, plugin hoặc tracker WordPress.
- Không sao chép mã CSS/JS Chemlabs vào runtime. Raw HTML giữ riêng để nghiên cứu.
- Không dùng ảnh stock của demo Chemlabs. Ảnh Anpha có nguồn trong manifest; quyền xuất bản cần chủ sở hữu xác nhận.
- Lexend/Roboto self-hosted, giấy phép OFL lưu cùng thư mục font.
- QA trang chính ghi nhận 0 request ngoài localhost, 0 response lỗi, 0 JavaScript/console error.
- Media và font cục bộ; không phụ thuộc CDN khi trình demo.
- Form chỉ validate và hiển thị xác nhận; không email, không ghi DB, không upload.
- Không có bí mật/API key hoặc thông tin xác thực của website gốc trong mã ứng dụng.
- Nội dung dài đã loại script, style, form, iframe, thuộc tính sự kiện và URL javascript trước khi render MarkupString. Kho lưu là snapshot tin cậy theo nguồn đã khảo sát; CMS production phải có pipeline sanitize riêng.
- Các URL bên ngoài trong nội dung gốc chỉ là liên kết điều hướng; không tự gọi để thực hiện nghiệp vụ.
- 4 link Anpha và 4 link Chemlabs trả 404 được lưu trong route-map; không dùng làm luồng chính.
- Tin tức năm 2016, thông tin hãng/giá/địa chỉ cũ và các mục đang cập nhật phải được khách duyệt.
- Trang `/de-xuat` và kho `/noi-dung` phục vụ review. Production cần quyết định bỏ hoặc chặn index cùng bảng redirect cho URL cũ.
- Chưa triển khai public. Bản demo không được coi là website production hoàn chỉnh.
