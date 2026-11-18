# SQL Server AnphaHealth

Database dùng SQL Server cục bộ, kết nối bằng tài khoản Windows. SQL là nguồn định nghĩa cấu trúc. Entity và DbContext EF Core được scaffold từ database.

```powershell
pwsh -File ./database/Initialize-AnphaHealth.ps1
sqlcmd -S localhost -E -C -b -i database/verify-import.sql
```

Máy cần SQL Server chạy, `sqlcmd` trong PATH, PowerShell 7 và quyền tạo database trên instance đích cho lần đầu. Truyền `-Server 'localhost\SQLEXPRESS'` nếu dùng named instance. Sau đó đặt connection string ứng dụng theo cùng instance.

```text
Server=localhost;Database=AnphaHealth;Integrated Security=True;TrustServerCertificate=True
```

[`schema.sql`](schema.sql) tạo database và các bảng. [`Initialize-AnphaHealth.ps1`](Initialize-AnphaHealth.ps1) kiểm tra snapshot đã crawl trước khi nạp. Dữ liệu ghi trong một giao dịch và chỉ chèn bản ghi thiếu. Chạy lại giữ các trường đã chỉnh sửa trong CMS và khôi phục bản ghi nguồn bị xóa nếu khóa nguồn còn trong snapshot. Script seed không tạo mật khẩu quản trị.

| Bảng | Dữ liệu khởi tạo |
|---|---|
| `ContentPages` | 177 URL công khai, với HTML đã làm sạch, văn bản và metadata nguồn |
| `ProductCategories`, `Products` | 4 ngành hàng và 8 sản phẩm theo danh mục ở bản demo |
| `MediaAssets`, `ContentPageMedia` | 127 ảnh và 726 liên kết giữa trang với ảnh |
| `SiteSettings` | 7 thiết lập thương hiệu và liên hệ |
| `SourceSnapshots` | 320 bản gốc gồm HTML, JSON WordPress, đối chiếu crawl và ảnh. Mỗi bản có checksum SHA-256 |
| `AdminUsers`, `ContactRequests` | Tài khoản CMS và yêu cầu liên hệ do ứng dụng tạo |

Website sử dụng nội dung đã chuẩn hóa trong các bảng CMS, và ảnh được phục vụ theo đường dẫn tương ứng trong `wwwroot/media`. Bản sao trong `SourceSnapshots` phục vụ đối chiếu và lưu trữ. File HTML gốc đã crawl được giữ riêng với HTML đã làm sạch mà ứng dụng sử dụng.

## Sinh lại model EF

Sau khi áp dụng thay đổi SQL vào database, chạy từ thư mục gốc:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef dbcontext scaffold 'Name=ConnectionStrings:AnphaHealth' Microsoft.EntityFrameworkCore.SqlServer `
    --project src/AnphaHealth.Web --output-dir Data/Entities --context-dir Data `
    --namespace AnphaHealth.Web.Data.Entities --context-namespace AnphaHealth.Web.Data `
    --context AnphaHealthDbContext --schema dbo --no-onconfiguring --force
```

Cần công cụ `dotnet-ef` 10.x. Các phần mở rộng giữ riêng trong file partial. Khi checkout không có `RECON`, script vẫn tạo đủ trang, danh mục, ảnh và bài viết từ JSON. Đưa thêm `RECON` vào workspace và chạy lại để nhập bản gốc còn thiếu.
