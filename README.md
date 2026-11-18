# Anpha Health

Website public và CMS dùng Blazor Web App (.NET 10), EF Core Database First và SQL Server. Nội dung bắt đầu từ bản crawl anphahealth.vn. CMS dùng Tiptap để soạn thảo trang, bài viết và mô tả sản phẩm.

## Chạy trên máy này

```powershell
./start-demo.ps1
```

Website chạy ở http://127.0.0.1:5080. Mở http://127.0.0.1:5080/admin để vào CMS. Database `AnphaHealth` đã được tạo và nạp trên SQL Server `localhost` bằng Windows Authentication. Tài khoản quản trị cục bộ là `anpha-admin`. Mật khẩu ngẫu nhiên nằm trong `.local/cms-admin.txt`, được bỏ qua bởi Git.

## Khởi tạo trên máy khác

Cần .NET SDK 10, SQL Server, `sqlcmd` và PowerShell 7. Để nạp cả bản gốc, sao chép thư mục khảo sát `RECON` từ workspace hiện tại. Script cũng hỗ trợ checkout chỉ có JSON và media đã theo dõi trong Git.

```powershell
pwsh -File ./database/Initialize-AnphaHealth.ps1
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/AnphaHealth.Web --no-launch-profile -- --create-admin <ten-quan-tri>
./start-demo.ps1
```

Ứng dụng hỏi mật khẩu từ 12 ký tự và chỉ lưu hash trong SQL Server. Đặt `ConnectionStrings__AnphaHealth` khi dùng instance khác. Bản upload mới nằm ở `src/AnphaHealth.Web/App_Data/uploads` khi chạy từ source. `Media__UploadDirectory` cấu hình nơi lưu ảnh để duy trì qua các lần triển khai.

## Database First và tầng dịch vụ

- `database/schema.sql` định nghĩa schema. `Initialize-AnphaHealth.ps1` nhập dữ liệu theo khóa nguồn và giữ các chỉnh sửa đã lưu.
- `src/AnphaHealth.Web/Data` chứa `AnphaHealthDbContext` và các entity do EF Core scaffold từ SQL Server. Thay đổi schema trước rồi sinh lại model theo hướng dẫn trong [tài liệu SQL](database/README.md).
- `Data/Repository` có `IRepository<TEntity>` và triển khai EF. Mỗi thao tác dùng context riêng qua `IDbContextFactory`. Việc lưu sản phẩm, nội dung liên kết và quan hệ media diễn ra trong một lần lưu EF.
- `Services/Contracts` gồm interface nghiệp vụ và interface generic `IEditorService<TListItem,TInput>`. CMS dùng DTO ở `Services/Models`. Các Razor component gọi interface qua DI.
- `Components/Admin` chứa layout responsive, bảng điều khiển, trang/bài viết, danh mục/sản phẩm, thư viện ảnh, cài đặt và hộp thư tư vấn.
- `data` chứa JSON dùng để import. Website truy vấn dữ liệu đã xuất bản từ SQL Server.

## CMS và tài nguyên frontend

Quản trị viên có thể tạo, sửa, lưu nháp và xuất bản. Nội dung mới đi vào trang đọc nội dung hoặc tin tức theo loại. Các trang thiết kế riêng trên website giữ bố cục của giao diện Blazor. Phần giới thiệu, hoạt động và tuyển dụng đọc nội dung CMS. Trang chủ, danh mục, tin tức và liên hệ lấy dữ liệu hiển thị từ các bảng tương ứng.

Form liên hệ lưu yêu cầu vào hộp thư CMS. Trạng thái gồm chưa đọc, đã đọc và đã xử lý. Gửi email tự động chưa được cấu hình.

Phiên quản trị có thời hạn 8 giờ. Ảnh upload hỗ trợ JPEG, PNG, GIF và WebP tối đa 8 MB. Editor hỗ trợ tiêu đề, định dạng, danh sách, liên kết, ảnh và bảng. Gói Tiptap được lưu sẵn. Khi chỉnh mã editor, chạy:

```powershell
npm ci
npm run build:cms
```

```powershell
dotnet build src/AnphaHealth.Web
npm run qa
```

Chạy kiểm thử trình duyệt khi website đang mở trên cổng 5080. `scripts/recon.mjs` và `scripts/crawl.py` dùng cho khảo sát và làm mới dữ liệu nguồn. [Phạm vi báo giá ban đầu](docs/PHAM_VI_BAO_GIA.md) ghi nhận giai đoạn trình giao diện trước khi CMS được bổ sung.
