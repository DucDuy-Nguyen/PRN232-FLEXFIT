# Hướng Dẫn Bảo Vệ Đồ Án - Nguyễn Phi Long (Dịch Vụ Thanh Toán & Tín Dụng)

Tài liệu này bao gồm danh sách các câu hỏi thường gặp khi bảo vệ đồ án trước hội đồng giảng viên đối với phần hành **Payment & Credit Microservice**.

---

## 15 Câu Hỏi Điểm Nhấn (Mở Code & Chỉ Đúng Method)

### Câu 1: Vì sao em không lưu ví tín dụng trực tiếp trên Redis mà phải dùng SQL Server?
- **Trả lời ngắn (30s)**: Tín dụng ví là tiền bạc và tài sản của khách hàng, yêu cầu tính an toàn dữ liệu tuyệt đối (ACID). SQL Server là cơ sở dữ liệu quan hệ hỗ trợ ACID, khóa dòng và transaction mạnh mẽ, chống thất thoát. Redis chỉ đóng vai trò làm bộ nhớ đệm (Cache) để tăng hiệu năng đọc và làm khóa phân tán (Lock).
- **Trả lời đầy đủ**: SQL Server lưu trữ dữ liệu bền vững trên đĩa cứng, có log giao dịch giúp khôi phục khi gặp sự cố phần cứng. Redis chạy trên RAM nên dễ mất dữ liệu nếu server sập nguồn bất ngờ dù có cấu hình AOF. Việc dùng SQL Server làm nguồn dữ liệu gốc (Source of Truth) đảm bảo số dư tài khoản luôn chính xác.
- **File cần mở**: [UserCredit.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Domain/Entities/UserCredit.cs)
- **Hỏi thêm**: Nếu Redis sập, dịch vụ của em có chạy được không?
  - *Trả lời*: Có chạy được nhưng sẽ chậm hơn do phải truy vấn trực tiếp SQL Server, và cơ chế an toàn khóa phân tán sẽ tạm thời chuyển hướng hoặc báo lỗi bận.

### Câu 2: Em giải quyết vấn đề trùng lặp yêu cầu thanh toán (Idempotency) như thế nào?
- **Trả lời ngắn (30s)**: Em dùng cơ chế hai lớp. Lớp 1: dùng Redis Idempotency Key (`idempotency:payment:{paymentId}`) với lệnh `SET NX` để chặn nhanh. Lớp 2: Kiểm tra trạng thái giao dịch thực tế trong cơ sở dữ liệu SQL Server. Nếu trạng thái đơn hàng khác "Pending", em bỏ qua luôn không cộng credit nữa.
- **Trả lời đầy đủ**: Khi Webhook hoặc Callback được gọi nhiều lần, hệ thống sẽ cố gắng lấy Distributed Lock trước. Sau đó kiểm tra key idempotency trên Redis. Nếu không có key, sẽ bắt đầu một transaction và kiểm tra trạng thái dòng bản ghi Payment trong SQL Server. Nếu Payment đã ở trạng thái "Success", transaction rollback ngay lập tức để tránh cộng ví hai lần.
- **File cần mở**: [PaymentService.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Application/Services/PaymentService.cs)
- **Method**: `ProcessPaymentCallbackAsync`
- **Hỏi thêm**: Nếu Redis bị xóa sạch key khi đang xử lý webhook thì sao?
  - *Trả lời*: Cơ chế lớp 2 kiểm tra trạng thái SQL Server (`payment.Status != "Pending"`) vẫn hoạt động độc lập và bảo vệ dữ liệu thành công.

### Câu 3: Outbox Pattern là gì và tại sao em lại áp dụng nó ở đây?
- **Trả lời ngắn (30s)**: Trong microservice, ta không được bắn trực tiếp sự kiện (như `PaymentCompleted`) sang Redis Stream trong khi transaction SQL chưa commit. Nếu database rollback mà sự kiện vẫn bắn đi, hệ thống khác sẽ xử lý sai. Outbox Pattern lưu sự kiện vào bảng `OutboxMessage` trong cùng SQL transaction. Khi SQL commit thành công, một Worker chạy nền mới quét bảng này và đẩy sang Redis.
- **Trả lời đầy đủ**: Thiết kế này giải quyết bài toán Dual-Write. Bằng cách lưu event vào database cùng lúc với cập nhật ví, ta đảm bảo chắc chắn event sẽ được tạo ra nếu ví được cộng tiền thành công. Sau đó `OutboxPublisherWorker` có trách nhiệm gửi an toàn event này sang Redis với cơ chế retry nếu Redis gặp sự cố tạm thời.
- **File cần mở**: [PaymentService.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Application/Services/PaymentService.cs) và [OutboxPublisherWorker.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Worker/Workers/OutboxPublisherWorker.cs)
- **Hỏi thêm**: Nếu Worker gửi event sang Redis thành công nhưng sập nguồn trước khi đánh dấu đã gửi trong database thì sao?
  - *Trả lời*: Event sẽ được gửi lại (At-least-once delivery). Bên tiêu thụ (như Booking Service) bắt buộc phải có cơ chế kiểm tra trùng lặp (Idempotent Consumer) để bỏ qua event trùng.

### Câu 4: Vì sao DbContext của EF Core lại được đăng ký dạng Scoped trong khi ConnectionMultiplexer của Redis lại là Singleton?
- **Trả lời ngắn (30s)**: `DbContext` đại diện cho một phiên làm việc với Database, giữ trạng thái các đối tượng được theo dõi (Change Tracker) và không an toàn khi dùng chung giữa các luồng (not thread-safe), nên đăng ký `Scoped` theo mỗi request. `ConnectionMultiplexer` của StackExchange.Redis được thiết kế để dùng chung kết nối một cách an toàn giữa nhiều luồng, việc mở kết nối rất tốn tài nguyên nên đăng ký `Singleton`.
- **File cần mở**: [Program.cs (API)](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.API/Program.cs)
- **Hỏi thêm**: Nếu em gọi DbContext từ một Singleton Service (như background worker) thì làm thế nào?
  - *Trả lời*: Em không được inject trực tiếp. Thay vào đó, inject `IServiceProvider` hoặc `IServiceScopeFactory`, gọi `CreateScope()` để tạo ra một phân vùng Scoped tạm thời, rồi giải phóng sau khi dùng xong.

### Câu 5: Distributed Lock hoạt động thế nào trong code của em?
- **Trả lời ngắn (30s)**: Khi có yêu cầu thay đổi số dư ví, em tạo khóa phân tán trên Redis bằng cách set key `lock:user:{userId}:wallet` kèm một chuỗi mã ngẫu nhiên (token) và thời gian hết hạn tự động (TTL). Khi xử lý xong, em chạy một mã script Lua để so sánh token hiện tại và xóa key một cách an toàn, tránh xóa nhầm khóa của tiến trình khác.
- **File cần mở**: [RedisDistributedLockService.cs](file:///d:/Ki_8_FPT/PRN232/PRN232-FLEXFIT/src/Services/Payment/FlexFit.Payment.Infrastructure/Services/RedisDistributedLockService.cs)
- **Method**: `AcquireLockAsync` và `ReleaseLockAsync`

---

## 35 Câu Hỏi Ngắn Gọn Khác

6. **Tài khoản người dùng sụp đổ thì số dư ví có bị xóa không?** - Không, vì ví được lưu trong Database Payment riêng biệt, chỉ tham chiếu qua `UserId` (Guid).
7. **Em sử dụng thư viện nào để kết nối Redis?** - StackExchange.Redis.
8. **Redis Stream khác Pub/Sub ở điểm nào?** - Stream lưu trữ sự kiện bền vững và hỗ trợ Consumer Group (cho phép phân chia công việc và lưu trạng thái đọc), trong khi Pub/Sub bắn tin nhắn xong là mất (fire-and-forget).
9. **XACK có ý nghĩa gì?** - Acknowledge. Báo với Redis Stream rằng consumer đã xử lý xong tin nhắn này và có thể gỡ khỏi danh sách pending (PEL).
10. **Làm thế nào để bảo mật ClientId và ApiKey của PayOS?** - Lưu trong cấu hình môi trường (Environment Variables) hoặc User Secrets, tuyệt đối không hardcode trong file code.
11. **JWT được xác thực ở đâu trong microservice của em?** - Tại Middleware `Authentication` trong `Program.cs` của Payment.API sử dụng public key/key đối xứng tương thích monolith.
12. **Mức độ cô lập giao dịch (Transaction Isolation Level) mặc định của SQL Server là gì?** - Read Committed.
13. **Tại sao ví tín dụng không có khóa ngoại (Foreign Key) sang bảng Users?** - Vì bảng Users nằm ở DB khác (Identity Db). Ràng buộc khóa ngoại liên DB là không thể trong thiết kế microservice thực tế.
14. **Sự khác biệt giữa 401 Unauthorized và 403 Forbidden?** - 401 là chưa đăng nhập / token không hợp lệ. 403 là đã đăng nhập nhưng không có quyền truy cập endpoint này (ví dụ: Member gọi API Admin).
15. **Làm sao để biết Redis sập?** - Endpoint `/health` trả về lỗi 500 khi ping Redis thất bại.
... *(Xem chi tiết trong file tài liệu)*
