# Hướng Dẫn Setup Database cho FlexFit Payment Service

Tài liệu này hướng dẫn các cách để khởi tạo và cấu hình database `FlexFitPaymentDb` cho Payment Service.

## 1. Cách chạy database bằng Docker (Khuyên dùng cho Development)

Nếu bạn chưa có sẵn SQL Server, bạn có thể chạy qua Docker nhanh chóng:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

## 2. Cách tạo database bằng file SQL

Đây là cách nhanh nhất để tạo đầy đủ schema và seed data mà không cần cài đặt EF Core CLI.

1. Mở **SQL Server Management Studio (SSMS)** hoặc **Azure Data Studio**.
2. Kết nối đến server SQL của bạn (ví dụ `localhost,1433`).
3. Mở file `docs/FlexFitPaymentDb_Database.sql`.
4. Nhấn **Execute** (hoặc F5) để chạy script.
5. (Tùy chọn) Mở file `docs/FlexFitPaymentDb_SeedData.sql` và nhấn **Execute** để thêm các gói Credit demo.

## 3. Cách chạy EF Core Migration

Nếu bạn muốn khởi tạo database bằng Entity Framework Core:

1. Mở terminal tại thư mục gốc của project (nơi chứa file `.slnx` hoặc `.sln`).
2. Chạy lệnh sau để apply migration vào database:

```bash
dotnet ef database update -p src/Services/Payment/FlexFit.Payment.Infrastructure/FlexFit.Payment.Infrastructure.csproj -s src/Services/Payment/FlexFit.Payment.API/FlexFit.Payment.API.csproj
```

*(Lưu ý: Bạn cần cấu hình ConnectionString trong `appsettings.json` hoặc User Secrets trước khi chạy lệnh này).*

## 4. Connection Strings

Thay đổi cấu hình trong file `appsettings.Development.json` hoặc sử dụng biến môi trường tùy thuộc vào môi trường chạy.

**Khi chạy SQL Server Local (chạy trực tiếp trên máy host):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=FlexFitPaymentDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;"
}
```

**Khi chạy SQL Server bằng Docker (từ máy host kết nối vào Docker):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=FlexFitPaymentDb;User Id=sa;Password=YourStrong@Passw0rd;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;"
}
```

**Khi chạy cả Payment API và SQL Server trong Docker Compose (kết nối giữa các container):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=sqlserver,1433;Database=FlexFitPaymentDb;User Id=sa;Password=YourStrong@Passw0rd;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;"
}
```
*(Thay `sqlserver` bằng tên service của SQL Server trong file docker-compose.yml).*

## 5. Cách kiểm tra database đã tạo thành công

1. Dùng SSMS kết nối vào database server.
2. Kiểm tra xem database `FlexFitPaymentDb` đã xuất hiện chưa.
3. Mở thư mục `Tables`, xác nhận có đầy đủ các bảng sau:
   - `CreditPackages`
   - `UserCredits`
   - `CreditTransactions`
   - `Payments`
   - `OutboxMessages`
   - `ProcessedMessages`
   - `__EFMigrationsHistory`
4. Click chuột phải vào `CreditPackages` -> **Select Top 1000 Rows** để xem dữ liệu seed (các gói Credit) đã được thêm thành công chưa.

## 6. Cách rollback hoặc xóa database để tạo lại

### Xóa database qua SQL Server
Chạy lệnh sau trong SSMS (master database):

```sql
USE master;
GO
ALTER DATABASE [FlexFitPaymentDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO
DROP DATABASE [FlexFitPaymentDb];
GO
```
Sau đó bạn có thể chạy lại file `docs/FlexFitPaymentDb_Database.sql` để tạo lại từ đầu.

### Rollback bằng EF Core
Nếu bạn dùng EF Core và muốn rollback lại trước khi apply migration:

```bash
dotnet ef database update 0 -p src/Services/Payment/FlexFit.Payment.Infrastructure/FlexFit.Payment.Infrastructure.csproj -s src/Services/Payment/FlexFit.Payment.API/FlexFit.Payment.API.csproj
```
*(Lệnh này sẽ xóa toàn bộ các bảng do migration tạo ra).*
