# Hướng Dẫn Thực Hành Demo - Dịch Vụ Thanh Toán & Tín Dụng

Tài liệu này hướng dẫn cách chạy thử nghiệm và demo các tính năng của Dịch vụ Thanh toán & Tín dụng cho giảng viên xem.

## 1. Chuẩn Bị Môi Trường
- **Yêu cầu cài đặt**: Docker Desktop, .NET 8 SDK, SQL Server Management Studio (hoặc Azure Data Studio), và Redis Insight (tùy chọn).

## 2. Các Bước Khởi Chạy Hệ Thống

### Bước 2.1 Chạy Docker Compose
Khởi động SQL Server và Redis chứa trong Docker:
```bash
docker compose up -d payment-db payment-redis
```

### Bước 2.2 Áp dụng Migration Database
Chạy lệnh migrations EF Core để khởi tạo bảng dữ liệu:
```bash
dotnet ef database update --project src/Services/Payment/FlexFit.Payment.Infrastructure/FlexFit.Payment.Infrastructure.csproj --startup-project src/Services/Payment/FlexFit.Payment.API/FlexFit.Payment.API.csproj --context FlexFit.Payment.Infrastructure.Data.PaymentDbContext
```

### Bước 2.3 Khởi chạy Web API và Worker
Chạy ứng dụng API:
```bash
dotnet run --project src/Services/Payment/FlexFit.Payment.API/FlexFit.Payment.API.csproj
```
Chạy Worker nền tiêu thụ sự kiện:
```bash
dotnet run --project src/Services/Payment/FlexFit.Payment.Worker/FlexFit.Payment.Worker.csproj
```

---

## 3. Các Kịch Bản Demo Từng Bước

### Kịch Bản 1: Xem danh sách gói nạp (Get Credit Packages)
- **API**: `GET http://localhost:5080/api/payment/packages` hoặc thông qua giao diện Swagger UI `/swagger`.
- **Mong đợi**: Nhận về danh sách 3 gói mặc định (Bronze, Silver, Gold) đã được seed tự động lúc startup.

### Kịch Bản 2: Tạo link thanh toán PayOS (Create Payment)
- **API**: `POST http://localhost:5080/api/payment/create`
- **Request Body**:
  ```json
  {
    "packageId": "<Bronze_Package_Guid>",
    "paymentMethod": "PAYOS"
  }
  ```
- **Mong đợi**: Nhận về `checkoutUrl` chứa cổng thanh toán VietQR của PayOS.

### Kịch Bản 3: Mô phỏng Webhook thành công từ PayOS
Để test webhook nội bộ không cần mạng ngoài, ta có thể mô phỏng bằng cách gọi callback trực tiếp (hoặc gửi payload webhook):
- **API**: `POST http://localhost:5080/api/payment/callback?paymentId=<PaymentId>&status=Success`
- **Mong đợi**:
  - Trả về thông báo thành công.
  - Số dư ví người dùng được cộng chính xác.
  - Kiểm tra SQL Server: bản ghi `UserCredits` được cập nhật, `CreditTransactions` ghi nhận biến động, `OutboxMessages` ghi nhận sự kiện `PaymentCompleted`.

### Kịch Bản 4: Khấu trừ tín dụng do đặt lịch (Booking Deduction)
Để giả lập Booking Service gửi yêu cầu khấu trừ tín dụng qua Redis:
1. Sử dụng công cụ Redis CLI hoặc Redis Insight gửi tin nhắn vào stream `flexfit:booking:events`:
   ```bash
   XADD flexfit:booking:events * EventType CreditDeductionRequested Payload '{"bookingId":"f7481cb0-c23f-42e1-a066-51e967a149c4","userId":"<UserId_Guid>","creditCost":30,"referenceType":"GymBooking","description":"Dat open gym"}'
   ```
2. Quan sát log của **FlexFit.Payment.Worker**:
   - Worker phát hiện tin nhắn, lấy ví người dùng, trừ 30 credit, ghi outbox.
   - Database SQL cập nhật ví người dùng còn giảm đi 30.
   - Bản ghi outbox `CreditDeductionSucceeded` được đẩy sang stream `flexfit:credit:events` cho Booking Service.
