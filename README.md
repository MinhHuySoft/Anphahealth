# Anpha Health — bản demo Blazor

Giao diện trình khách, dùng nội dung công khai từ **anphahealth.vn** và hướng thiết kế **Chemlabs / 7oroof**. Mã giao diện được viết lại bằng Blazor, CSS riêng; ảnh và font lưu cục bộ.

## Chạy demo

Yêu cầu .NET SDK 10 (repo pin 10.0.201).

```powershell
./start-demo.ps1
```

Mở http://127.0.0.1:5080. Trang `/de-xuat` có phạm vi demo, bảng ghép template và danh sách dữ liệu đã crawl.

```powershell
dotnet build src/AnphaHealth.Web
dotnet publish src/AnphaHealth.Web -c Release -o artifacts/publish
```

## Cấu trúc

- `src/AnphaHealth.Web`: Blazor Web App .NET 10, Interactive Server.
- `data/anpha-content.json`: 177 trang đã trích nội dung, HTML được làm sạch.
- `data/asset-manifest.json`: 127 tệp media cục bộ, URL nguồn và dung lượng.
- `RECON/raw`: HTML gốc của Anpha, HTML tham chiếu Chemlabs và toàn bộ dữ liệu REST pages/posts/media.
- `RECON/routes`: kiểm kê URL của hai website, HTTP status và lỗi nguồn.
- `RECON/screenshots`: ảnh đối chiếu nguồn và demo ở desktop/tablet/mobile.
- `docs/PHAM_VI_BAO_GIA.md`: phạm vi gửi khách và các hạng mục cần chốt giá.
- `CLONE_REPORT.md`: kết quả triển khai và kiểm tra thực tế.

## Crawl lại

```powershell
python -m pip install -r requirements.txt
python -X utf8 scripts/crawl.py
```

Crawl các endpoint REST công khai, theo hết liên kết nội bộ tìm được, tôn trọng robots.txt, lưu HTML và tải media gốc. 124 bản ghi media có metadata của các ảnh thumbnail; lưu 127 tệp ảnh gốc/thực dùng, không tải lặp toàn bộ thumbnail sinh tự động. Không thu thập nội dung riêng tư, không gửi form và không sao chép tracker vào app.

## Kiểm tra trình duyệt

Khởi động demo trước, sau đó:

```powershell
npm ci
npx playwright install chromium
npm run qa
```

Script dùng Chromium đã có trong cache Windows nếu tìm thấy, hoặc Chromium của Playwright trên nền tảng khác. Lưu kết quả trong `RECON/clone-qa.json`. `scripts/recon.mjs` dùng cho khảo sát hai website nguồn.

## Giới hạn bản demo

Form chỉ xem trước, không gửi email và không lưu thông tin. Chưa có CMS, đăng nhập, cơ sở dữ liệu, upload, thanh toán hay hệ thống nghiệp vụ. Các mục đang cập nhật ở website gốc giữ đúng trạng thái. Thông tin cũ, giá, thông số và địa chỉ phải được khách duyệt trước khi đưa lên production.

Chemlabs là template thương mại của 7oroof. Nếu dùng mã/asset của template khi triển khai chính thức cần license phù hợp. Demo hiện dùng component/CSS tự viết; ảnh Anpha được giữ để trình khách và cần được chủ sở hữu xác nhận quyền sử dụng khi xuất bản. Không có thao tác triển khai public trong dự án này.
