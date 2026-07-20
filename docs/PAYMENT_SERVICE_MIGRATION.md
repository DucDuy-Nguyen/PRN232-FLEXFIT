# Kế Hoạch Chuyển Đổi Hệ Thống (Migration & Cutover Plan)

Tài liệu này đề xuất chiến lược chuyển đổi từ hệ thống Monolith sang Microservice Payment & Credit một cách an toàn và không gây gián đoạn dịch vụ.

## 1. Cơ Chế Tách DB Và Tách Dữ Liệu
- Hệ thống thanh toán mới sử dụng Database riêng (`india02_payment`).
- Dữ liệu lịch sử thanh toán và số dư ví của khách hàng từ Database monolith sẽ được di chuyển sang Database mới bằng Script di trú (Migration Script).
- **Chú ý**: Không xóa ngay bảng ví và thanh toán trong Monolith Database cho đến khi Microservice chạy ổn định ít nhất 2 tuần.

## 2. API Gateway & Điều Hướng (Routing Proposal)
Chúng tôi đề xuất sử dụng **YARP (Yet Another Reverse Proxy)** hoặc **Ocelot** làm Gateway ở cổng đầu vào:

### Quy Tắc Điều Hướng (Routes)
- Bất kỳ request nào bắt đầu bằng `/api/payment/**` hoặc `/api/credit-packages/**` sẽ được Gateway chuyển tiếp trực tiếp sang **Payment Microservice** (`http://payment-api:8080`).
- Các request nghiệp vụ khác giữ nguyên chuyển hướng sang Monolith API.

## 3. Chiến Lược Cắt Chuyển An Toàn (Cutover Strategy)
Sử dụng **Feature Flag** trong cấu hình Monolith (`appsettings.json`):
```json
{
  "PaymentIntegration": {
    "UseMicroservice": true
  }
}
```

### Cách Hoạt Động Của Flag:
- **Nếu `UseMicroservice = false`**: 
  - Monolith tiếp tục gọi Repo nội bộ truy vấn thẳng Database cũ của Monolith.
- **Nếu `UseMicroservice = true`**:
  - Lớp Service trong Monolith (ví dụ: `BookingService`) sẽ không gọi repo ví nội bộ nữa, mà sẽ bắn sự kiện `CreditDeductionRequested` vào Redis Stream `flexfit:booking:events` và lắng nghe sự kiện trả về bất đồng bộ.
  - Phía Client UI sẽ được chỉ định chuyển hướng gọi API nạp tiền sang Gateway địa chỉ mới của Microservice Payment.
- **Lợi ích**: Có thể quay xe (Rollback) lập tức về trạng thái Monolith bằng cách đổi flag về `false` nếu Microservice gặp sự cố chịu tải trong ngày đầu chạy thật.
