# Cấu Hình Và Sử Dụng Redis - Payment Microservice

Redis đóng vai trò là hạ tầng quan trọng hỗ trợ Payment Microservice chạy chịu tải cao, đồng thời đảm bảo an toàn giao dịch tài chính.

## 1. Các Key Caching
- **Danh sách gói nạp active**:
  - `payment:packages:active` (Hết hạn sau 1 giờ)
- **Số dư ví của người dùng**:
  - `payment:user:{userId}:balance` (Hết hạn sau 30 phút, bị xóa ngay lập tức khi số dư thay đổi trong SQL Server).
- **Tóm tắt doanh thu Admin**:
  - `payment:admin:revenue_summary` (Hết hạn sau 10 phút, bị xóa ngay khi có bản ghi thanh toán mới hoàn tất thành công).

## 2. Distributed Locks (Khóa Phân Tán)
Để tránh hiện tượng tranh chấp ví (race condition) khi nhiều tiến trình trừ tiền hoặc cộng tiền cùng một lúc cho một user:
- **Key cấu trúc**: `lock:user:{userId}:wallet`
- **Thời gian TTL mặc định**: 15 giây.
- **Giá trị**: Token ngẫu nhiên (Guid) giúp giải phóng khóa an toàn bằng Script Lua.

## 3. Idempotency Keys (Chặn Trùng Giao Dịch)
- **Key cấu trúc**: `idempotency:payment:{paymentId}`
- **Thời gian TTL**: 24 giờ.
- **Cơ chế**: Dùng lệnh `SET NX` để xác nhận giao dịch chưa từng được xử lý trong vòng 24 giờ qua.

## 4. Redis Streams (Nhắn Tin Sự Kiện)
Hệ thống sử dụng các Stream sau để đồng bộ hóa bất đồng bộ:
- **`flexfit:booking:events`**: Nghe các yêu cầu đặt lịch/hoàn lịch (`CreditDeductionRequested`, `CreditRefundRequested`) từ Booking Service.
  - **Consumer Group**: `payment-service`
  - **Consumer Name**: Tự động sinh ngẫu nhiên khi Worker khởi chạy.
- **`flexfit:payment:events`**: Nơi Payment Service bắn ra sự kiện `PaymentCompleted`, `PaymentFailed` để các service khác tiêu thụ.
- **`flexfit:credit:events`**: Nơi Payment Service bắn ra kết quả xử lý tín dụng `CreditDeductionSucceeded` hoặc `CreditDeductionFailed` để Booking Service tiếp tục luồng đặt lịch.
- **`flexfit:dead-letter`**: Nơi lưu giữ các sự kiện bị lỗi quá số lần cấu hình (3 lần) để kiểm tra thủ công.
