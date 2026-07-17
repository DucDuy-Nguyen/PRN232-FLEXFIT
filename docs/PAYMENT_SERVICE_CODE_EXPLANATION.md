# Giải Thích Mã Nguồn - Dịch Vụ Thanh Toán & Tín Dụng (Payment & Credit Service)

Tài liệu này giải thích chi tiết mã nguồn thực tế của Microservice Thanh toán & Tín dụng do **Nguyễn Phi Long** phụ trách.

## 1. Vai Trò Của Nguyễn Phi Long
- Thiết kế và phát triển độc lập hệ thống Microservice **Payment & Credit** tách rời khỏi monolith.
- Xây dựng hệ thống giao dịch tài chính an toàn sử dụng SQL Server làm nguồn dữ liệu tin cậy (Source of Truth), kết hợp Redis làm hạ tầng phụ trợ: stream sự kiện, khóa phân tán (Distributed Lock), bộ đệm (Cache) và đảm bảo tính duy nhất (Idempotency).
- Tích hợp cổng thanh toán trực tuyến **PayOS** và các tiến trình nền tự động (Background Workers).

## 2. Kiến Trúc Tổng Thể & Các Dự Án (Projects)
Hệ thống sử dụng **Kiến Trúc Sạch (Clean Architecture)** được phân chia thành 6 dự án con:

### A. FlexFit.Payment.Domain
- **Vị trí**: `src/Services/Payment/FlexFit.Payment.Domain`
- **Nhiệm vụ**: Định nghĩa các thực thể cốt lõi của miền nghiệp vụ (Entities), không phụ thuộc vào bất kỳ thư viện bên ngoài nào.
- **Thành phần**:
  - [CreditPackage.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Domain/Entities/CreditPackage.cs): Định nghĩa gói nạp tín dụng.
  - [Payment.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Domain/Entities/Payment.cs): Trạng thái đơn nạp tiền.
  - [UserCredit.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Domain/Entities/UserCredit.cs): Ví tín dụng của người dùng.
  - [CreditTransaction.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Domain/Entities/CreditTransaction.cs): Lịch sử giao dịch biến động số dư.
  - [OutboxMessage.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Domain/Entities/OutboxMessage.cs): Sự kiện tài chính cần gửi đi.

### B. FlexFit.Payment.Contracts
- **Vị trí**: `src/Services/Payment/FlexFit.Payment.Contracts`
- **Nhiệm vụ**: Chứa các cấu trúc sự kiện (Events) dùng chung giữa các dịch vụ.
- **Thành phần**:
  - `CreditDeductionRequested`, `CreditDeductionSucceeded`, `CreditRefundRequested`, `PaymentCompleted`, v.v.

### C. FlexFit.Payment.Application
- **Vị trí**: `src/Services/Payment/FlexFit.Payment.Application`
- **Nhiệm vụ**: Chứa các interface trừu tượng và dịch vụ nghiệp vụ ứng dụng.
- **Thành phần**:
  - Các interface: `IPaymentService`, `ICreditService`, `IDistributedLockService`, `ICacheService`, `IPayOSPaymentGateway`.
  - Các dịch vụ: `PaymentService.cs`, `CreditService.cs`, `CreditAdjustmentService.cs`.

### D. FlexFit.Payment.Infrastructure
- **Vị trí**: `src/Services/Payment/FlexFit.Payment.Infrastructure`
- **Nhiệm vụ**: Cài đặt chi tiết các interface của lớp Application (truy cập cơ sở dữ liệu, bộ cài đặt Redis, cổng PayOS).
- **Thành phần**:
  - [PaymentDbContext.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Infrastructure/Data/PaymentDbContext.cs): Cấu hình EF Core.
  - [RedisService.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Infrastructure/Services/RedisService.cs): Cài đặt cache, lock, idempotency bằng Redis.
  - [PayOSPaymentGateway.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Infrastructure/Services/PayOSPaymentGateway.cs): Đóng gói cổng thanh toán PayOS.

### E. FlexFit.Payment.API
- **Vị trí**: `src/Services/Payment/FlexFit.Payment.API`
- **Nhiệm vụ**: Cổng giao tiếp HTTP REST API.
- **Thành phần**:
  - [PaymentController.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.API/Controllers/PaymentController.cs): API nạp tiền, callback, webhook PayOS.
  - [CreditPackageController.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.API/Controllers/CreditPackageController.cs): Quản lý gói nạp và kiểm tra số dư ví.

### F. FlexFit.Payment.Worker
- **Vị trí**: `src/Services/Payment/FlexFit.Payment.Worker`
- **Nhiệm vụ**: Tiến trình chạy nền tiêu thụ sự kiện từ Redis Streams và phát hành Outbox events.
- **Thành phần**:
  - [RedisConsumerWorker.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Worker/Workers/RedisConsumerWorker.cs)
  - [OutboxPublisherWorker.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Worker/Workers/OutboxPublisherWorker.cs)

---

## 3. Các Luồng Nghiệp Vụ Quan Trọng

### Luồng Trừ Tín Dụng (Deduction Flow)
1. **Booking Service** gửi sự kiện `CreditDeductionRequested` vào stream `flexfit:booking:events`.
2. **RedisConsumerWorker** trong Payment Service nhận được thông báo:
   - Yêu cầu khóa phân tán ví người dùng (`lock:user:{UserId}:wallet`).
   - Kiểm tra trùng lặp giao dịch (Idempotency).
   - Bắt đầu SQL Transaction: kiểm tra số dư tín dụng -> thực hiện trừ tiền -> ghi lịch sử `CreditTransaction` -> ghi sự kiện `CreditDeductionSucceeded` hoặc `CreditDeductionFailed` vào bảng `OutboxMessages`.
   - Lưu cơ sở dữ liệu và commit SQL Transaction.
   - Giải phóng khóa ví người dùng.
   - Xóa cache ví của người dùng.
   - Trả xác nhận `XACK` về Redis Stream.

### Quy Trình Outbox Pattern
Để đảm bảo tính nhất quán dữ liệu (Data Consistency) giữa SQL Server và Redis Streams, mọi sự kiện tài chính không được bắn trực tiếp vào Redis khi đang chạy SQL transaction. Thay vào đó, chúng được ghi vào bảng `OutboxMessages` trong cùng một transaction của database.
Sau khi SQL commit thành công, **OutboxPublisherWorker** sẽ quét định kỳ các bản ghi chưa xử lý trong bảng Outbox, gửi chúng vào Redis Streams tương ứng và đánh dấu đã xử lý.
