using System;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);

        // Đặt lịch thành công
        Task SendGymBookingSuccessEmailAsync(string toEmail, string customerName, string sessionName, string branchName, DateTime startTime, DateTime endTime, string bookingCode);
        Task SendClassBookingSuccessEmailAsync(string toEmail, string customerName, string className, string branchName, DateTime startTime, DateTime endTime, string bookingCode);

        // Hủy lịch
        Task SendGymBookingCancelledEmailAsync(string toEmail, string customerName, string sessionName, string branchName, DateTime startTime, DateTime endTime, string bookingCode);
        Task SendClassBookingCancelledEmailAsync(string toEmail, string customerName, string className, string branchName, DateTime startTime, DateTime endTime, string bookingCode);

        // ========================================================
        // TÁCH BIỆT HÀM NHẮC LỊCH CHO GYM VÀ CLASS
        // ========================================================
        Task SendGymBookingReminderEmailAsync(string toEmail, string customerName, string sessionName, string branchName, DateTime startTime, DateTime endTime, string bookingCode, int hoursLeft);
        Task SendClassBookingReminderEmailAsync(string toEmail, string customerName, string className, string branchName, DateTime startTime, DateTime endTime, string bookingCode, int hoursLeft);
    }
}