using Flexfit.Helpers;
using Flexfit.Service;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class BookingReminderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingReminderWorker> _logger;

        public BookingReminderWorker(IServiceScopeFactory scopeFactory, ILogger<BookingReminderWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flexfit Booking Reminder Worker đã khởi động thành công.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong quá trình quét tự động nhắc lịch.");
                }
            }
        }

        private async Task ProcessRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTimeHelper.GetVietnamTime();
            bool hasChanges = false;

            // ========================================================
            // 1. QUÉT LỊCH TẬP GYM (GYM BOOKINGS)
            // ========================================================

            // --- MỐC 3 TIẾNG (Chỉ lấy lịch còn từ > 2h45 đến <= 3h) ---
            var gym3h = await bookingRepo.GetGymBookingsToRemindAsync(now, hoursLeft: 3);
            foreach (var booking in gym3h)
            {
                var timeDiff = booking.Session.StartTime - now;

                // Nếu thời gian còn lại nhỏ hơn hoặc bằng 2h45 tiếng -> BỎ QUA mốc 3h để mốc 1h xử lý
                if (timeDiff.TotalHours <= 2.75)
                {
                    continue;
                }

                string email = booking.User?.Email ?? "khachhang@flexfit.com";
                string name = booking.User?.FullName ?? "Hội viên Flexfit";

                await emailService.SendGymBookingReminderEmailAsync(
                    email, name, "Lịch Tập Gym Tự Do", "Chi nhánh Flexfit Fitness",
                    booking.Session.StartTime, booking.Session.EndTime, booking.BookingCode, hoursLeft: 3
                );
                booking.IsReminded3h = true;
                await bookingRepo.UpdateGymBookingAsync(booking);
                hasChanges = true;
            }

            // --- MỐC 1 TIẾNG (Lấy lịch còn <= 1h) ---
            var gym1h = await bookingRepo.GetGymBookingsToRemindAsync(now, hoursLeft: 1);
            foreach (var booking in gym1h)
            {
                var timeDiff = booking.Session.StartTime - now;

                // Điều kiện nghiêm ngặt: Nhỏ hơn hoặc bằng 1 tiếng và lịch chưa diễn ra (TotalHours > 0)
                if (timeDiff.TotalHours <= 1.0 && timeDiff.TotalHours > 0)
                {
                    string email = booking.User?.Email ?? "khachhang@flexfit.com";
                    string name = booking.User?.FullName ?? "Hội viên Flexfit";

                    await emailService.SendGymBookingReminderEmailAsync(
                        email, name, "Lịch Tập Gym Tự Do", "Chi nhánh Flexfit Fitness",
                        booking.Session.StartTime, booking.Session.EndTime, booking.BookingCode, hoursLeft: 1
                    );
                    booking.IsReminded1h = true;
                    // Đánh dấu true luôn cho mốc 3h để sau này hệ thống không quét lại nữa nếu có biến động
                    booking.IsReminded3h = true;
                    await bookingRepo.UpdateGymBookingAsync(booking);
                    hasChanges = true;
                }
            }

            // ========================================================
            // 2. QUÉT LỊCH LỚP HỌC (CLASS BOOKINGS)
            // ========================================================

            // --- MỐC 3 TIẾNG (Chỉ lấy lịch còn từ > 2h45 đến <= 3h) ---
            var class3h = await bookingRepo.GetClassBookingsToRemindAsync(now, hoursLeft: 3);
            foreach (var booking in class3h)
            {
                var timeDiff = booking.Class.StartTime - now;

                // Nếu thời gian còn lại <= 2h45 tiếng -> BỎ QUA mốc 3h
                if (timeDiff.TotalHours <= 2.75)
                {
                    continue;
                }

                string email = booking.User?.Email ?? "khachhang@flexfit.com";
                string name = booking.User?.FullName ?? "Hội viên Flexfit";

                await emailService.SendClassBookingReminderEmailAsync(
                    email, name, "Lớp Học Nhóm (Group Class)", "Chi nhánh Flexfit Studio",
                    booking.Class.StartTime, booking.Class.EndTime, booking.BookingCode, hoursLeft: 3
                );
                booking.IsReminded3h = true;
                await bookingRepo.UpdateClassBookingAsync(booking);
                hasChanges = true;
            }

            // --- MỐC 1 TIẾNG (Lấy lịch còn <= 1h) ---
            var class1h = await bookingRepo.GetClassBookingsToRemindAsync(now, hoursLeft: 1);
            foreach (var booking in class1h)
            {
                var timeDiff = booking.Class.StartTime - now;

                // Điều kiện nghiêm ngặt: Nhỏ hơn hoặc bằng 1 tiếng và lớp chưa diễn ra
                if (timeDiff.TotalHours <= 1 && timeDiff.TotalHours > 0)
                {
                    string email = booking.User?.Email ?? "khachhang@flexfit.com";
                    string name = booking.User?.FullName ?? "Hội viên Flexfit";

                    await emailService.SendClassBookingReminderEmailAsync(
                        email, name, "Lớp Học Nhóm (Group Class)", "Chi nhánh Flexfit Studio",
                        booking.Class.StartTime, booking.Class.EndTime, booking.BookingCode, hoursLeft: 1
                    );
                    booking.IsReminded1h = true;
                    booking.IsReminded3h = true; // Đóng luôn cờ mốc 3h
                    await bookingRepo.UpdateClassBookingAsync(booking);
                    hasChanges = true;
                }
            }

            // Lưu thay đổi trạng thái cờ vào DB
            if (hasChanges)
            {
                await bookingRepo.SaveChangesAsync();
            }
        }
    }
}