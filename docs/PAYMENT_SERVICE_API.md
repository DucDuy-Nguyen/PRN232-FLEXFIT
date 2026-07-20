# Tài Liệu API - Dịch Vụ Thanh Toán & Tín Dụng

Tất cả các endpoint HTTP REST API của dịch vụ được định nghĩa dưới đây:

## 1. Nhóm API Thanh Toán (Payment Controller)

### 1.1 Lấy danh sách gói nạp hoạt động
- **Endpoint**: `GET /api/payment/packages`
- **Xác thực**: Không yêu cầu
- **Trả về**:
  ```json
  [
    {
      "packageId": "guid-string",
      "packageName": "Gói Đồng (Bronze)",
      "creditAmount": 100,
      "bonusCredit": 0,
      "price": 100000.0,
      "description": "...",
      "isPopular": false,
      "isActive": true
    }
  ]
  ```

### 1.2 Tạo liên kết thanh toán
- **Endpoint**: `POST /api/payment/create`
- **Xác thực**: Yêu cầu JWT (Bearer token)
- **Body**:
  ```json
  {
    "packageId": "guid-string-package-id",
    "paymentMethod": "PAYOS" // PAYOS, VNPAY, MOMO, MOCK
  }
  ```
- **Trả về**:
  ```json
  {
    "paymentId": "guid-string",
    "paymentUrl": "https://payos.checkout/...",
    "status": "Pending",
    "amount": 100000.0
  }
  ```

### 1.3 Nhận phản hồi thanh toán (Callback)
- **Endpoint**: `POST /api/payment/callback`
- **Xác thực**: Không yêu cầu
- **Query Parameters**:
  - `paymentId` (Guid)
  - `status` (Success/Failed)
  - `providerTransactionCode` (string)

---

## 2. Nhóm API Ví Tín Dụng (Credit Package Controller)

### 2.1 Xem số dư ví tín dụng người dùng
- **Endpoint**: `GET /api/credit-packages/wallet/{userId}`
- **Xác thực**: Yêu cầu JWT (Bearer)
- **Trả về**:
  ```json
  {
    "userId": "guid-string",
    "balance": 150,
    "updatedAt": "datetime"
  }
  ```

### 2.2 Đơn phương cộng tín dụng bởi Admin (Adjustment)
- **Endpoint**: `POST /api/credit-packages/admin/adjust`
- **Xác thực**: Chỉ cho phép tài khoản Admin (`[Authorize(Roles = "Admin")]`)
- **Body**:
  ```json
  {
    "userId": "guid-string-user",
    "amount": 50,
    "description": "Den bu su co he thong"
  }
  ```

---

## 3. Nhóm API Báo Cáo Doanh Thu (Admin Revenue Controller)

### 3.1 Báo cáo doanh thu và xu hướng
- **Endpoint**: `GET /api/admin/revenue/summary`
- **Xác thực**: Chỉ Admin (`[Authorize(Roles = "Admin")]`)
- **Trả về**: Tổng quan doanh thu theo ngày, tháng, biểu đồ xu hướng 6 tháng qua và xếp hạng gói bán chạy nhất.
