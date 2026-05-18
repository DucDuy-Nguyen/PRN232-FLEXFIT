using Flexfit.Service;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["EmailSettings:Email"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["EmailSettings:Email"], _config["EmailSettings:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        // ========================================================
        // 1. GỬI MAIL ĐẶT LỊCH GYM THÀNH CÔNG
        // ========================================================
        public async Task SendGymBookingSuccessEmailAsync(string toEmail, string customerName, string sessionName, string branchName, DateTime startTime, DateTime endTime, string bookingCode)
        {
            string safeCode = string.IsNullOrEmpty(bookingCode) ? "FLEXFIT" : (bookingCode.Length >= 8 ? bookingCode.Substring(0, 8) : bookingCode);

            string subject = $"[Flexfit] Xác nhận đặt Lịch Tập Gym thành công - #{safeCode.ToUpper()}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #2ecc71; text-align: center;'>Đăng Ký Khung Giờ Tập Gym Thành Công!</h2>
                    <p>Xin chào <b>{customerName}</b>,</p>
                    <p>Chúc mừng bạn đã đặt lịch tập Gym thành công. Dưới đây là thông tin chi tiết về ca tập của bạn:</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0; background: #f9f9f9; padding: 10px; border-radius: 5px;'>
                        <tr><td style='padding: 8px; color: #555; width: 35%;'>Mã đặt lịch:</td><td><b>{safeCode.ToUpper()}</b></td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Khung giờ tập:</td><td><b>{sessionName}</b></td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Chi nhánh:</td><td>{branchName}</td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Thời gian gian tập:</td><td style='color: #27ae60;'><b>{startTime:HH:mm} - {endTime:HH:mm}</b> ({startTime:dd/MM/yyyy})</td></tr>
                    </table>
                    <p style='margin-top: 20px; text-align: center; color: #888; font-size: 12px;'>Đừng quên mang theo bình nước cá nhân và khăn tập nhé. Hẹn gặp lại bạn tại phòng tập!</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        // ========================================================
        // 2. GỬI MAIL ĐẶT LỚP HỌC (CLASS) THÀNH CÔNG
        // ========================================================
        public async Task SendClassBookingSuccessEmailAsync(string toEmail, string customerName, string className, string branchName, DateTime startTime, DateTime endTime, string bookingCode)
        {
            string safeCode = string.IsNullOrEmpty(bookingCode) ? "FLEXFIT" : (bookingCode.Length >= 8 ? bookingCode.Substring(0, 8) : bookingCode);

            string subject = $"[Flexfit] Xác nhận đăng ký Lớp Học thành công - #{safeCode.ToUpper()}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #3498db; text-align: center;'>Giữ Chỗ Lớp Học Thành Công!</h2>
                    <p>Xin chào <b>{customerName}</b>,</p>
                    <p>Bạn đã giữ chỗ thành công cho lớp học tại Flexfit. Thông tin chi tiết lớp học của bạn như sau:</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0; border-left: 4px solid #3498db; background: #f4f8fb; padding: 10px;'>
                        <tr><td style='padding: 8px; color: #555; width: 35%;'>Mã đặt vé:</td><td><b>{safeCode.ToUpper()}</b></td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Tên lớp học:</td><td><b style='color: #2980b9;'>{className}</b></td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Địa điểm:</td><td>{branchName}</td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Thời gian lớp học:</td><td style='color: #e67e22;'><b>{startTime:HH:mm} - {endTime:HH:mm}</b> ({startTime:dd/MM/yyyy})</td></tr>
                    </table>
                    <p style='margin-top: 20px; text-align: center; color: #888; font-size: 12px;'>Vui lòng đến sớm trước 10 phút để điểm danh lớp học kịp thời. Chúc bạn một buổi tập năng lượng!</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        // ========================================================
        // 3. GỬI MAIL HỦY LỊCH GYM
        // ========================================================
        public async Task SendGymBookingCancelledEmailAsync(string toEmail, string customerName, string sessionName, string branchName, DateTime startTime, DateTime endTime, string bookingCode)
        {
            string safeCode = string.IsNullOrEmpty(bookingCode) ? "FLEXFIT" : (bookingCode.Length >= 8 ? bookingCode.Substring(0, 8) : bookingCode);

            string subject = $"[Flexfit] Thông báo hủy Lịch Tập Gym - #{safeCode.ToUpper()}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #e74c3c; text-align: center;'>Đã Hủy Lịch Tập Gym</h2>
                    <p>Xin chào <b>{customerName}</b>,</p>
                    <p>Hệ thống ghi nhận bạn đã hủy thành công ca tập <b>{sessionName}</b> tại chi nhánh <b>{branchName}</b>.</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0; background: #fff5f5; padding: 10px; border-radius: 5px; border: 1px solid #f9d5d5;'>
                        <tr><td style='padding: 8px; color: #555; width: 35%;'>Mã đặt lịch cũ:</td><td><b>{safeCode.ToUpper()}</b></td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Khung giờ đã hủy:</td><td style='color: #c0392b;'><b>{startTime:HH:mm} - {endTime:HH:mm}</b> ({startTime:dd/MM/yyyy})</td></tr>
                    </table>
                    <p>Rất tiếc vì bạn không thể tham gia ca tập này. Hãy quay lại ứng dụng Flexfit và chọn cho mình một khung giờ khác phù hợp hơn nhé!</p>
                    <p style='margin-top: 20px; text-align: center; color: #888; font-size: 12px;'>Flexfit luôn sẵn sàng đồng hành cùng bạn.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        // ========================================================
        // 4. GỬI MAIL HỦY LỚP HỌC (CLASS)
        // ========================================================
        public async Task SendClassBookingCancelledEmailAsync(string toEmail, string customerName, string className, string branchName, DateTime startTime, DateTime endTime, string bookingCode)
        {
            string safeCode = string.IsNullOrEmpty(bookingCode) ? "FLEXFIT" : (bookingCode.Length >= 8 ? bookingCode.Substring(0, 8) : bookingCode);

            string subject = $"[Flexfit] Thông báo hủy đăng ký Lớp Học - #{safeCode.ToUpper()}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: #e67e22; text-align: center;'>Hủy Đăng Ký Lớp Học Thành Công</h2>
                    <p>Xin chào <b>{customerName}</b>,</p>
                    <p>Bạn đã thực hiện hủy đăng ký tham gia lớp học <b>{className}</b> tại cơ sở <b>{branchName}</b>.</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0; background: #fffaf5; padding: 10px; border-radius: 5px; border: 1px solid #faebd7;'>
                        <tr><td style='padding: 8px; color: #555; width: 35%;'>Mã đặt vé cũ:</td><td><b>{safeCode.ToUpper()}</b></td></tr>
                        <tr><td style='padding: 8px; color: #555;'>Khung giờ đã hủy:</td><td style='color: #d35400;'><b>{startTime:HH:mm} - {endTime:HH:mm}</b> ({startTime:dd/MM/yyyy})</td></tr>
                    </table>
                    <p>Việc hủy lớp sớm của bạn đã giúp nhường cơ hội tham gia cho các hội viên khác. Số Credit (nếu có) sẽ được hoàn trả lại ví của bạn theo chính sách của phòng tập.</p>
                    <p style='margin-top: 20px; text-align: center; color: #888; font-size: 12px;'>Cảm ơn bạn đã thông báo kịp thời. Hẹn gặp lại bạn ở lớp học sau!</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        // ========================================================
        // 5. GỬI MAIL NHẮC LỊCH TẬP GYM RIÊNG BIỆT
        // ========================================================
        public async Task SendGymBookingReminderEmailAsync(string toEmail, string customerName, string sessionName, string branchName, DateTime startTime, DateTime endTime, string bookingCode, int hoursLeft)
        {
            string safeCode = string.IsNullOrEmpty(bookingCode) ? "FLEXFIT" : bookingCode;
            string titleText = hoursLeft <= 1 ? "SẮP ĐẾN GIỜ TẬP GYM (CÒN 1 TIẾNG)" : "NHẮC NHỞ LỊCH TẬP GYM (CÒN 3 TIẾNG)";
            string themeColor = "#2ecc71"; // Màu xanh lá khỏe khoắn cho Gym

            string subject = $"[Flexfit] Nhắc nhở: Ca tập Gym của bạn sẽ bắt đầu sau {hoursLeft} tiếng!";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 2px solid {themeColor}; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: {themeColor}; text-align: center;'>{titleText}</h2>
                    <p>Xin chào <b>{customerName}</b>,</p>
                    <p>Hệ thống nhắc bạn có lịch tập Gym tự do đã đăng ký sắp diễn ra. Hãy chuẩn bị trang phục thể thao sẵn sàng nhé!</p>
                    <div style='background: #fdfdfd; padding: 15px; border-radius: 5px; border-left: 4px solid {themeColor}; margin: 20px 0; background-color: #f4fbf7;'>
                        <p style='margin: 5px 0;'><b>Mã đặt lịch:</b> #{safeCode.ToUpper()}</p>
                        <p style='margin: 5px 0;'><b>Loại hình:</b> Thể hình tự do (Gym Session)</p>
                        <p style='margin: 5px 0;'><b>Khung ca tập:</b> {sessionName}</p>
                        <p style='margin: 5px 0;'><b>Chi nhánh:</b> {branchName}</p>
                        <p style='margin: 5px 0;'><b>Thời gian:</b> <span style='color: {themeColor}; font-weight: bold;'>{startTime:HH:mm} - {endTime:HH:mm}</span> ({startTime:dd/MM/yyyy})</p>
                    </div>
                    <p style='text-align: center; color: #777; font-size: 12px;'>Vui lòng mang theo khăn tập cá nhân khi đến phòng. Chúc bạn có một buổi tập hiệu quả!</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        // ========================================================
        // 6. GỬI MAIL NHẮC LỊCH LỚP HỌC (CLASS) RIÊNG BIỆT
        // ========================================================
        public async Task SendClassBookingReminderEmailAsync(string toEmail, string customerName, string className, string branchName, DateTime startTime, DateTime endTime, string bookingCode, int hoursLeft)
        {
            string safeCode = string.IsNullOrEmpty(bookingCode) ? "FLEXFIT" : bookingCode;
            string titleText = hoursLeft <= 1 ? "LỚP HỌC SẮP BẮT ĐẦU (CÒN 1 TIẾNG)" : "NHẮC NHỞ LỊCH LỚP HỌC (CÒN 3 TIẾNG)";
            string themeColor = "#e67e22"; // Màu cam năng động cho Group Class

            string subject = $"[Flexfit] Nhắc nhở: Lớp học nhóm [{className}] sẽ bắt đầu sau {hoursLeft} tiếng!";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 2px solid {themeColor}; padding: 20px; border-radius: 8px;'>
                    <h2 style='color: {themeColor}; text-align: center;'>{titleText}</h2>
                    <p>Xin chào <b>{customerName}</b>,</p>
                    <p>Bạn đã giữ chỗ thành công cho lớp học nhóm. Vui lòng sắp xếp thời gian đến sớm để check-in ổn định chỗ tập cùng Huấn luyện viên.</p>
                    <div style='background: #fdfdfd; padding: 15px; border-radius: 5px; border-left: 4px solid {themeColor}; margin: 20px 0; background-color: #fffbf5;'>
                        <p style='margin: 5px 0;'><b>Mã đặt vé:</b> #{safeCode.ToUpper()}</p>
                        <p style='margin: 5px 0;'><b>Tên lớp học:</b> <span style='color: #2980b9; font-weight: bold;'>{className}</span></p>
                        <p style='margin: 5px 0;'><b>Địa điểm:</b> {branchName}</p>
                        <p style='margin: 5px 0;'><b>Thời gian lớp:</b> <span style='color: {themeColor}; font-weight: bold;'>{startTime:HH:mm} - {endTime:HH:mm}</span> ({startTime:dd/MM/yyyy})</p>
                    </div>
                    <p style='text-align: center; color: #777; font-size: 12px;'>* Lưu ý: Hội viên đến muộn quá 5 phút sau khi lớp bắt đầu sẽ không được vào lớp để đảm bảo chất lượng bài học.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}