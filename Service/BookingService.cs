using Flexfit.DTOs;
using Flexfit.DTOs.Booking;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Flexfit.Repository;
using Flexfit.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly IPromotionRepository _promotionRepo;
        private readonly ISystemLogService _systemLogService;
        private readonly INotificationService _notificationService;

        public BookingService(
            IBookingRepository bookingRepo,
            IPromotionRepository promotionRepo,
            ISystemLogService systemLogService,
            INotificationService notificationService)
        {
            _bookingRepo = bookingRepo;
            _promotionRepo = promotionRepo;
            _systemLogService = systemLogService;
            _notificationService = notificationService;
        }

        private string GenerateBookingCode()
        {
            return "BK" + new Random().Next(100000, 999999).ToString();
        }

        private async Task<PromotionPreviewResponse> CalculatePromotionAsync(int originalCredit, DateTime now)
        {
            var safeOriginalCredit = Math.Max(0, originalCredit);
            var promotion = await _promotionRepo.GetBestActivePromotionAsync(now);

            if (promotion?.DiscountPercent is not int discountPercent)
            {
                return new PromotionPreviewResponse
                {
                    OriginalCredit = safeOriginalCredit,
                    FinalCredit = safeOriginalCredit
                };
            }

            var discountCredit = (int)Math.Floor(safeOriginalCredit * (discountPercent / 100.0));
            var finalCredit = Math.Max(0, safeOriginalCredit - discountCredit);

            return new PromotionPreviewResponse
            {
                OriginalCredit = safeOriginalCredit,
                DiscountPercent = discountPercent,
                DiscountCredit = discountCredit,
                FinalCredit = finalCredit,
                PromotionId = promotion.PromotionId,
                PromotionTitle = promotion.Title
            };
        }

        public Task<PromotionPreviewResponse> GetPromotionPreviewAsync(int originalCredit)
        {
            return CalculatePromotionAsync(originalCredit, DateTimeHelper.GetVietnamTime());
        }

        // ========================================================
        // HÀM DÙNG CHUNG PHÂN NHÓM TABS
        // ========================================================
        private Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>> GroupBookingsIntoTabs(
            IEnumerable<CustomerBookingHistoryResponse> bookingsList, DateTime now)
        {
            var active = bookingsList.Where(b => b.Status != "Cancelled" && b.StartTime <= now && b.EndTime >= now).OrderBy(b => b.StartTime);
            var upcoming = bookingsList.Where(b => b.Status != "Cancelled" && b.StartTime > now).OrderBy(b => b.StartTime);
            var past = bookingsList.Where(b => b.Status == "Cancelled" || b.EndTime < now).OrderByDescending(b => b.EndTime);

            return new Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>
            {
                { "Active", active },
                { "Upcoming", upcoming },
                { "Past", past }
            };
        }

        public async Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerGymBookingTabsAsync(Guid ownerId)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var gymBookings = await _bookingRepo.GetGymBookingsByOwnerIdAsync(ownerId);

            var mappedList = (gymBookings ?? Enumerable.Empty<GymBooking>()).Select(gb => new CustomerBookingHistoryResponse
            {
                BookingId = gb.BookingId,
                BookingCode = gb.BookingCode,
                BookingType = "GYM",
                Name = gb.Session?.SessionName ?? "Tập Gym Tự Do",
                BranchName = gb.Session?.Branch?.BranchName ?? "Chi nhánh Flexfit",
                StartTime = gb.Session?.StartTime ?? DateTime.MinValue,
                EndTime = gb.Session?.EndTime ?? DateTime.MinValue,
                CreditUsed = gb.CreditUsed,
                Status = gb.Status,
                CheckInStatus = gb.CheckInStatus,
                CheckInTime = gb.CheckInTime,
                CustomerName = gb.User?.FullName ?? "Hội viên Flexfit",
                CustomerEmail = gb.User?.Email ?? ""
            }).ToList();

            return GroupBookingsIntoTabs(mappedList, now);
        }

        public async Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerClassBookingTabsAsync(Guid ownerId)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var classBookings = await _bookingRepo.GetClassBookingsByOwnerIdAsync(ownerId);

            var mappedList = (classBookings ?? Enumerable.Empty<ClassBooking>()).Select(cb => new CustomerBookingHistoryResponse
            {
                BookingId = cb.BookingId,
                BookingCode = cb.BookingCode,
                BookingType = "CLASS",
                Name = cb.Class?.ClassName ?? "Lớp học thể thao",
                BranchName = cb.Class?.Branch?.BranchName ?? "Chi nhánh Flexfit",
                StartTime = cb.Class?.StartTime ?? DateTime.MinValue,
                EndTime = cb.Class?.EndTime ?? DateTime.MinValue,
                CreditUsed = cb.CreditUsed,
                Status = cb.Status,
                CheckInStatus = cb.CheckInStatus,
                CheckInTime = cb.CheckInTime,
                CustomerName = cb.User?.FullName ?? "Hội viên Flexfit",
                CustomerEmail = cb.User?.Email ?? ""
            }).ToList();

            return GroupBookingsIntoTabs(mappedList, now);
        }

        // ========================================================
        // 1. GYM SESSION BOOKING
        // ========================================================
        public async Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request)
        {
            var now = DateTimeHelper.GetVietnamTime();

            if (request.StartTime <= now) throw new Exception("Không thể đặt lịch cho thời gian trong quá khứ.");
            if (request.EndTime <= request.StartTime) throw new Exception("Thời gian kết thúc phải sau thời gian bắt đầu.");

            var session = await _bookingRepo.GetGymSessionByDetailsAsync(request.BranchId, request.SessionName, request.StartTime, request.EndTime);

            if (session == null)
            {
                var branch = await _bookingRepo.GetBranchByIdAsync(request.BranchId);
                if (branch == null) throw new Exception("Không tìm thấy chi nhánh.");

                session = new GymSession
                {
                    SessionId = Guid.NewGuid(),
                    BranchId = request.BranchId,
                    SessionName = request.SessionName,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Capacity = 100,
                    CreditCost = branch.CreditCost,
                    Status = "Active",
                    CreatedAt = now
                };
                await _bookingRepo.AddGymSessionAsync(session);
            }

            if (await _bookingRepo.CountGymBookingsBySessionIdAsync(session.SessionId) >= session.Capacity)
                throw new Exception("Session này đã hết chỗ.");

            var promotionPreview = await CalculatePromotionAsync(session.CreditCost, now);
            int finalCreditCost = promotionPreview.FinalCredit;

            var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
            if (userCredit == null || userCredit.Balance < finalCreditCost)
                throw new Exception($"Tài khoản không đủ credit. Cần {finalCreditCost} credit để đặt lịch.");

            int balanceBefore = userCredit.Balance;
            userCredit.Balance -= finalCreditCost;
            userCredit.TotalSpent += finalCreditCost;
            userCredit.UpdatedAt = now;

            var booking = new GymBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                SessionId = session.SessionId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = finalCreditCost,
                CheckInStatus = "Pending",
                Status = "Confirmed",
                BookedAt = now
            };

            await _bookingRepo.AddGymBookingAsync(booking);

            await _bookingRepo.AddCreditTransactionAsync(new CreditTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = userId,
                Amount = -finalCreditCost,
                BalanceBefore = balanceBefore,
                BalanceAfter = userCredit.Balance,
                Type = "Booking",
                ReferenceId = booking.BookingId,
                ReferenceType = "GymBooking",
                Description = promotionPreview.HasPromotion ? $"Đặt lịch tập Gym (Có giảm giá): {session.SessionName}" : $"Đặt lịch tập Gym thành công: {session.SessionName}",
                CreatedAt = now
            });

            await _bookingRepo.SaveChangesAsync();
            await _systemLogService.LogActionAsync(userId, "BOOK_GYM", $"Đặt lịch Gym: {session.SessionName}, Mã: {booking.BookingCode}", null);

            var detailedBooking = await _bookingRepo.GetGymBookingByIdAsync(booking.BookingId);

            // 🔔 GỬI THÔNG BÁO CHO CẢ USER VÀ OWNER PHÒNG GYM VÀ STAFF
            try
            {
                string branchName = detailedBooking?.Session?.Branch?.BranchName ?? "Chi nhánh hệ thống";
                string customerName = detailedBooking?.User?.FullName ?? "Hội viên Flexfit";

                // 1. Gửi cho Hội viên
                await _notificationService.SendAsync(userId, "Đặt lịch tập Gym thành công! 🎉",
                    $"Bạn đã đặt thành công Open Gym tại [{branchName}] lúc {session.StartTime:HH:mm dd/MM/yyyy}. Mã: {booking.BookingCode}.", "BookingSuccess");

                // 2. Gửi cho Owner
                var ownerId = detailedBooking?.Session?.Branch?.Gym?.OwnerId;
                if (ownerId.HasValue && ownerId.Value != Guid.Empty && ownerId.Value != userId)
                {
                    await _notificationService.SendAsync(ownerId.Value, "Có lịch đặt Gym mới! 🆕",
                        $"Khách hàng [{customerName}] vừa đặt lịch Open Gym - [{branchName}] vào lúc {session.StartTime:HH:mm dd/MM/yyyy}. Mã: {booking.BookingCode}.", "PartnerNotification");
                }

                // 3. Gửi cho Staff
                if (detailedBooking?.Session?.BranchId != null)
                {
                    var staffIds = await _bookingRepo.GetStaffIdsByBranchIdAsync(detailedBooking.Session.BranchId);
                    foreach (var staffId in staffIds)
                    {
                        if (staffId != userId && staffId != ownerId)
                        {
                            await _notificationService.SendAsync(staffId, "Có booking mới cần theo dõi",
                                $"Tại [{branchName}], khách [{customerName}] vừa đặt lịch Open Gym lúc {session.StartTime:HH:mm dd/MM/yyyy}. Mã: {booking.BookingCode}.", "StaffNotification");
                        }
                    }
                        // Broadcast to branch group for connected staff dashboards
                        try
                        {
                            await _notificationService.BroadcastToBranchAsync(detailedBooking.Session.BranchId, "Có booking mới cần theo dõi",
                                $"Tại [{branchName}], khách [{customerName}] vừa đặt lịch Open Gym lúc {session.StartTime:HH:mm dd/MM/yyyy}. Mã: {booking.BookingCode}.", "StaffNotification");
                        }
                        catch { }

            // Push credit balance update to the user if refund applied
            try
            {
                var userCreditCurrent = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCreditCurrent != null)
                {
                    await _notificationService.BroadcastCreditUpdateAsync(userId, userCreditCurrent.Balance);
                }
            }
            catch { }

            // Push credit balance update to the user if refund applied
            try
            {
                var userCreditCurrent = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCreditCurrent != null)
                {
                    await _notificationService.BroadcastCreditUpdateAsync(userId, userCreditCurrent.Balance);
                }
            }
            catch { }


                }
            }
            catch { }

            return new GymBookingResponse
            {
                BookingId = booking.BookingId,
                SessionId = booking.SessionId,
                SessionName = detailedBooking?.Session?.SessionName ?? request.SessionName,
                BranchName = detailedBooking?.Session?.Branch?.BranchName,
                GymName = detailedBooking?.Session?.Branch?.Gym?.GymName,
                StartTime = detailedBooking?.Session?.StartTime ?? request.StartTime,
                EndTime = detailedBooking?.Session?.EndTime ?? request.EndTime,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                OriginalCredit = promotionPreview.OriginalCredit,
                DiscountPercent = promotionPreview.DiscountPercent,
                DiscountCredit = promotionPreview.DiscountCredit,
                PromotionId = promotionPreview.PromotionId,
                BookedAt = booking.BookedAt,
                UserEmail = detailedBooking?.User?.Email ?? "",
                UserFullName = detailedBooking?.User?.FullName ?? "Hội viên Flexfit"
            };
        }

        public async Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetGymBookingsByUserIdAsync(userId);
            var reviewIds = await _bookingRepo.GetGymReviewIdsByBookingIdsAsync(bookings.Select(b => b.BookingId));
            return bookings.Select(b => new GymBookingResponse
            {
                BookingId = b.BookingId,
                SessionId = b.SessionId,
                SessionName = b.Session?.SessionName,
                BranchName = b.Session?.Branch?.BranchName,
                GymName = b.Session?.Branch?.Gym?.GymName,
                StartTime = b.Session?.StartTime ?? DateTime.MinValue,
                EndTime = b.Session?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                HasReview = reviewIds.ContainsKey(b.BookingId),
                ReviewId = reviewIds.TryGetValue(b.BookingId, out var reviewId) ? reviewId : null,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }

        public async Task<GymBookingResponse> CancelGymBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetGymBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId) throw new Exception("Booking không tồn tại hoặc lỗi quyền.");
            if (booking.Status == "Cancelled") throw new Exception("Booking này đã được huỷ trước đó.");

            var now = DateTimeHelper.GetVietnamTime();
            if (booking.Session != null && booking.Session.StartTime <= now) throw new Exception("Không thể huỷ khi session đã bắt đầu.");
            if (await _bookingRepo.GetCancellationCountTodayAsync(userId) >= 2) throw new Exception("Mỗi ngày hủy tối đa 2 lịch đặt.");

            double refundPercentage = 0;
            if (booking.Session != null)
            {
                var timeRemaining = booking.Session.StartTime - now;
                if (timeRemaining.TotalHours >= 12) refundPercentage = 100;
                else if (timeRemaining.TotalHours >= 6) refundPercentage = 70;
                else if (timeRemaining.TotalHours >= 3) refundPercentage = 50;
                else if (timeRemaining.TotalHours >= 1) refundPercentage = 25;
            }

            int refundAmount = (int)Math.Round(booking.CreditUsed * (refundPercentage / 100.0));
            booking.Status = "Cancelled";
            booking.CancelledAt = now;
            booking.RefundCredit = refundAmount;

            if (refundAmount > 0)
            {
                var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCredit != null)
                {
                    int balanceBefore = userCredit.Balance;
                    userCredit.Balance += refundAmount;
                    userCredit.TotalSpent = Math.Max(0, userCredit.TotalSpent - refundAmount);
                    userCredit.UpdatedAt = now;

                    await _bookingRepo.AddCreditTransactionAsync(new CreditTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId,
                        Amount = refundAmount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = userCredit.Balance,
                        Type = "Refund",
                        ReferenceId = booking.BookingId,
                        ReferenceType = "GymBooking",
                        Description = $"Hoàn credit hủy Gym ({refundPercentage}%). Khung giờ: {booking.Session?.SessionName}",
                        CreatedAt = now
                    });
                }
            }

            await _bookingRepo.UpdateGymBookingAsync(booking);
            await _bookingRepo.SaveChangesAsync();
            await _systemLogService.LogActionAsync(userId, "CANCEL_GYM", $"Hủy lịch Gym mã: {booking.BookingCode}", null);

            // 🔔 GỬI THÔNG BÁO HỦY LỊCH CHO USER, OWNER VÀ STAFF
            try
            {
                string sessionName = booking.Session?.SessionName ?? "Lịch tập Gym";
                string branchName = booking.Session?.Branch?.BranchName ?? "Chi nhánh Flexfit";
                string customerName = booking.User?.FullName ?? "Hội viên Flexfit";

                // 1. Gửi cho Hội viên
                await _notificationService.SendAsync(userId, "Hủy lịch tập Gym thành công ↩️",
                    $"Đã hủy khung giờ [{sessionName}]. Hoàn {refundAmount} Credits.", "BookingCancelled");

                // 2. Gửi cho Owner
                var ownerId = booking.Session?.Branch?.Gym?.OwnerId;
                if (ownerId.HasValue && ownerId.Value != Guid.Empty && ownerId.Value != userId)
                {
                    await _notificationService.SendAsync(ownerId.Value, "Hội viên đã hủy lịch đặt Gym ⚠️",
                        $"Khách hàng [{customerName}] đã hủy lịch đặt khung giờ [{sessionName}] tại [{branchName}]. Mã đơn: {booking.BookingCode}.", "PartnerNotification");
                }

                // 3. Gửi cho Staff
                if (booking.Session?.BranchId != null)
                {
                    var staffIds = await _bookingRepo.GetStaffIdsByBranchIdAsync(booking.Session.BranchId);
                    foreach (var staffId in staffIds)
                    {
                        if (staffId != userId && staffId != ownerId)
                        {
                            await _notificationService.SendAsync(staffId, "Booking Gym đã bị hủy",
                                $"Khách hàng [{customerName}] đã hủy lịch đặt khung giờ [{sessionName}] tại [{branchName}]. Mã: {booking.BookingCode}.", "StaffNotification");
                        }
                    }
                }
            }
            catch { }

            // Push credit balance update to the user (real-time)
            try
            {
                var userCreditCurrent = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCreditCurrent != null)
                {
                    await _notificationService.BroadcastCreditUpdateAsync(userId, userCreditCurrent.Balance);
                }
            }
            catch { }

            return new GymBookingResponse
            {
                BookingId = booking.BookingId,
                SessionId = booking.SessionId,
                SessionName = booking.Session?.SessionName,
                BranchName = booking.Session?.Branch?.BranchName,
                GymName = booking.Session?.Branch?.Gym?.GymName,
                StartTime = booking.Session?.StartTime ?? DateTime.MinValue,
                EndTime = booking.Session?.EndTime ?? DateTime.MinValue,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt,
                UserEmail = booking.User?.Email ?? "",
                UserFullName = booking.User?.FullName ?? ""
            };
        }

        // ========================================================
        // 2. CLASS BOOKING
        // ========================================================
        public async Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var classObj = await _bookingRepo.GetClassByIdAsync(request.ClassId);
            if (classObj == null) throw new Exception("Class không tồn tại.");
            if (classObj.StartTime <= now) throw new Exception("Không thể đặt lịch cho lớp đã bắt đầu hoặc kết thúc.");

            if (await _bookingRepo.CountClassBookingsByClassIdAsync(classObj.ClassId) >= classObj.Capacity)
                throw new Exception("Lớp học này đã hết chỗ.");

            var promotionPreview = await CalculatePromotionAsync(classObj.CreditCost, now);
            int finalCreditCost = promotionPreview.FinalCredit;

            var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
            if (userCredit == null || userCredit.Balance < finalCreditCost)
                throw new Exception($"Tài khoản không đủ credit. Cần {finalCreditCost} credit để đặt lịch.");

            int balanceBefore = userCredit.Balance;
            userCredit.Balance -= finalCreditCost;
            userCredit.TotalSpent += finalCreditCost;
            userCredit.UpdatedAt = now;

            var booking = new ClassBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                ClassId = classObj.ClassId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = finalCreditCost,
                CheckInStatus = "Pending",
                Status = "Confirmed",
                BookedAt = now
            };

            await _bookingRepo.AddClassBookingAsync(booking);

            await _bookingRepo.AddCreditTransactionAsync(new CreditTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = userId,
                Amount = -finalCreditCost,
                BalanceBefore = balanceBefore,
                BalanceAfter = userCredit.Balance,
                Type = "Booking",
                ReferenceId = booking.BookingId,
                ReferenceType = "ClassBooking",
                Description = promotionPreview.HasPromotion ? $"Đặt lịch Class (Có giảm giá): {classObj.ClassName}" : $"Đặt lịch Class thành công: {classObj.ClassName}",
                CreatedAt = now
            });

            await _bookingRepo.SaveChangesAsync();
            await _systemLogService.LogActionAsync(userId, "BOOK_CLASS", $"Đặt lịch lớp {classObj.ClassName}. Mã: {booking.BookingCode}", null);

            var detailedBooking = await _bookingRepo.GetClassBookingByIdAsync(booking.BookingId);

            // 🔔 GỬI THÔNG BÁO CHO CẢ USER, OWNER VÀ STAFF KHI ĐẶT CLASS
            try
            {
                string branchName = detailedBooking?.Class?.Branch?.BranchName ?? "Chi nhánh hệ thống";
                string customerName = detailedBooking?.User?.FullName ?? "Hội viên Flexfit";
                string coachText = !string.IsNullOrEmpty(classObj.CoachName) ? $" cùng HLV {classObj.CoachName}" : "";

                // 1. Gửi cho Hội viên
                await _notificationService.SendAsync(userId, "Đặt lớp học thành công! 🧘‍♂️",
                    $"Lớp [{classObj.ClassName}]{coachText}. Học lúc: {classObj.StartTime:HH:mm dd/MM/yyyy}. Mã vé: {booking.BookingCode}.", "BookingSuccess");

                // 2. Gửi cho Owner
                var ownerId = detailedBooking?.Class?.Branch?.Gym?.OwnerId;
                if (ownerId.HasValue && ownerId.Value != Guid.Empty && ownerId.Value != userId)
                {
                    await _notificationService.SendAsync(ownerId.Value, "Có lịch đặt lớp học mới! 🆕",
                        $"Khách hàng [{customerName}] đã đăng ký lớp [{classObj.ClassName}] tại [{branchName}] vào lúc {classObj.StartTime:HH:mm dd/MM/yyyy}. Mã: {booking.BookingCode}.", "PartnerNotification");
                }

                // 3. Gửi cho Staff
                if (detailedBooking?.Class?.BranchId != null)
                {
                    var staffIds = await _bookingRepo.GetStaffIdsByBranchIdAsync(detailedBooking.Class.BranchId);
                    foreach (var staffId in staffIds)
                    {
                        if (staffId != userId && staffId != ownerId)
                        {
                            await _notificationService.SendAsync(staffId, "Có booking lớp học mới",
                                $"Tại [{branchName}], khách [{customerName}] vừa đặt lớp [{classObj.ClassName}] lúc {classObj.StartTime:HH:mm dd/MM/yyyy}. Mã: {booking.BookingCode}.", "StaffNotification");
                        }
                    }
                    // Broadcast to branch group for connected staff dashboards
                    try
                    {
                        await _notificationService.BroadcastToBranchAsync(detailedBooking.Class.BranchId, "Có booking lớp học mới",
                            $"Tại [{branchName}], khách [{customerName}] vừa đặt lớp [{classObj.ClassName}] lúc {classObj.StartTime:HH:mm dd/MM/yyyy}. Mã: {booking.BookingCode}.", "StaffNotification");
                    }
                    catch { }
                }
            }
            catch { }

            // Broadcast updated class capacity to connected clients viewing this class
            try
            {
                var bookedCount = await _bookingRepo.CountClassBookingsByClassIdAsync(classObj.ClassId);
                var remaining = Math.Max(0, classObj.Capacity - bookedCount);
                await _notificationService.BroadcastClassCapacityAsync(classObj.ClassId, remaining);
            }
            catch { }

            // Push credit balance update to the user (real-time)
            try
            {
                var userCreditCurrent = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCreditCurrent != null)
                {
                    await _notificationService.BroadcastCreditUpdateAsync(userId, userCreditCurrent.Balance);
                }
            }
            catch { }



            return new ClassBookingResponse
            {
                BookingId = booking.BookingId,
                ClassId = booking.ClassId,
                ClassName = detailedBooking?.Class?.ClassName ?? classObj.ClassName,
                CoachName = detailedBooking?.Class?.CoachName,
                BranchName = detailedBooking?.Class?.Branch?.BranchName,
                GymName = detailedBooking?.Class?.Branch?.Gym?.GymName,
                StartTime = detailedBooking?.Class?.StartTime ?? classObj.StartTime,
                EndTime = detailedBooking?.Class?.EndTime ?? classObj.EndTime,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                OriginalCredit = promotionPreview.OriginalCredit,
                DiscountPercent = promotionPreview.DiscountPercent,
                DiscountCredit = promotionPreview.DiscountCredit,
                PromotionId = promotionPreview.PromotionId,
                BookedAt = booking.BookedAt,
                UserEmail = detailedBooking?.User?.Email ?? "",
                UserFullName = detailedBooking?.User?.FullName ?? "Hội viên Flexfit"
            };
        }

        public async Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetClassBookingsByUserIdAsync(userId);
            var reviewIds = await _bookingRepo.GetClassReviewIdsByBookingIdsAsync(bookings.Select(b => b.BookingId));
            return bookings.Select(b => new ClassBookingResponse
            {
                BookingId = b.BookingId,
                ClassId = b.ClassId,
                ClassName = b.Class?.ClassName,
                CoachName = b.Class?.CoachName,
                BranchName = b.Class?.Branch?.BranchName,
                GymName = b.Class?.Branch?.Gym?.GymName,
                StartTime = b.Class?.StartTime ?? DateTime.MinValue,
                EndTime = b.Class?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                HasReview = reviewIds.ContainsKey(b.BookingId),
                ReviewId = reviewIds.TryGetValue(b.BookingId, out var reviewId) ? reviewId : null,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }

        public async Task<ClassBookingResponse> CancelClassBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetClassBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId) throw new Exception("Booking không tồn tại hoặc lỗi quyền.");
            if (booking.Status == "Cancelled") throw new Exception("Booking này đã được huỷ trước đó.");

            var now = DateTimeHelper.GetVietnamTime();
            if (booking.Class != null && booking.Class.StartTime <= now) throw new Exception("Không thể huỷ khi lớp học đã bắt đầu.");
            if (await _bookingRepo.GetCancellationCountTodayAsync(userId) >= 2) throw new Exception("Mỗi ngày hủy tối đa 2 lịch đặt.");

            double refundPercentage = 0;
            if (booking.Class != null)
            {
                var timeRemaining = booking.Class.StartTime - now;
                if (timeRemaining.TotalHours >= 12) refundPercentage = 100;
                else if (timeRemaining.TotalHours >= 6) refundPercentage = 70;
                else if (timeRemaining.TotalHours >= 3) refundPercentage = 50;
                else if (timeRemaining.TotalHours >= 1) refundPercentage = 25;
            }

            int refundAmount = (int)Math.Round(booking.CreditUsed * (refundPercentage / 100.0));
            booking.Status = "Cancelled";
            booking.CancelledAt = now;
            booking.RefundCredit = refundAmount;

            if (refundAmount > 0)
            {
                var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCredit != null)
                {
                    int balanceBefore = userCredit.Balance;
                    userCredit.Balance += refundAmount;
                    userCredit.TotalSpent = Math.Max(0, userCredit.TotalSpent - refundAmount);
                    userCredit.UpdatedAt = now;

                    await _bookingRepo.AddCreditTransactionAsync(new CreditTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId,
                        Amount = refundAmount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = userCredit.Balance,
                        Type = "Refund",
                        ReferenceId = booking.BookingId,
                        ReferenceType = "ClassBooking",
                        Description = $"Hoàn credit hủy Class ({refundPercentage}%). Lớp: {booking.Class?.ClassName}",
                        CreatedAt = now
                    });
                }
            }

            await _bookingRepo.UpdateClassBookingAsync(booking);
            await _bookingRepo.SaveChangesAsync();
            await _systemLogService.LogActionAsync(userId, "CANCEL_CLASS", $"Hủy lịch Class mã: {booking.BookingCode}", null);

            // 🔔 GỬI THÔNG BÁO HỦY LỚP CHO USER, OWNER VÀ STAFF
            try
            {
                string className = booking.Class?.ClassName ?? "Lớp học";
                string branchName = booking.Class?.Branch?.BranchName ?? "Chi nhánh Flexfit";
                string customerName = booking.User?.FullName ?? "Hội viên Flexfit";

                // 1. Gửi cho Hội viên
                await _notificationService.SendAsync(userId, "Hủy lớp học thành công ↩️",
                    $"Đã hủy thành công lịch học lớp [{className}]. Hoàn {refundAmount} Credits.", "BookingCancelled");

                // 2. Gửi cho Owner
                var ownerId = booking.Class?.Branch?.Gym?.OwnerId;
                if (ownerId.HasValue && ownerId.Value != Guid.Empty && ownerId.Value != userId)
                {
                    await _notificationService.SendAsync(ownerId.Value, "Hội viên đã hủy lịch học Class ⚠️",
                        $"Khách hàng [{customerName}] đã hủy đăng ký lớp [{className}] tại chi nhánh [{branchName}]. Mã vé: {booking.BookingCode}.", "PartnerNotification");
                }

                // 3. Gửi cho Staff
                if (booking.Class?.BranchId != null)
                {
                    var staffIds = await _bookingRepo.GetStaffIdsByBranchIdAsync(booking.Class.BranchId);
                    foreach (var staffId in staffIds)
                    {
                        if (staffId != userId && staffId != ownerId)
                        {
                            await _notificationService.SendAsync(staffId, "Booking lớp học đã bị hủy",
                                $"Khách hàng [{customerName}] đã hủy đăng ký lớp [{className}] tại [{branchName}]. Mã: {booking.BookingCode}.", "StaffNotification");
                        }
                    }
                    try
                    {
                        await _notificationService.BroadcastToBranchAsync(booking.Class.BranchId, "Booking lớp học đã bị hủy",
                            $"Khách hàng [{customerName}] đã hủy đăng ký lớp [{className}] tại [{branchName}]. Mã: {booking.BookingCode}.", "StaffNotification");
                    }
                    catch { }
                }
            }
            catch { }

            return new ClassBookingResponse
            {
                BookingId = booking.BookingId,
                ClassId = booking.ClassId,
                ClassName = booking.Class?.ClassName,
                CoachName = booking.Class?.CoachName,
                BranchName = booking.Class?.Branch?.BranchName,
                GymName = booking.Class?.Branch?.Gym?.GymName,
                StartTime = booking.Class?.StartTime ?? DateTime.MinValue,
                EndTime = booking.Class?.EndTime ?? DateTime.MinValue,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt,
                UserEmail = booking.User?.Email ?? "",
                UserFullName = booking.User?.FullName ?? ""
            };
        }

        // ========================================================
        // 3. PARTNER METHODS (GET RAW)
        // ========================================================
        public async Task<IEnumerable<GymBookingResponse>> GetPartnerGymBookingsAsync(Guid ownerId)
        {
            var bookings = await _bookingRepo.GetGymBookingsByOwnerIdAsync(ownerId);
            return bookings.Select(b => new GymBookingResponse
            {
                BookingId = b.BookingId,
                SessionId = b.SessionId,
                SessionName = b.Session?.SessionName,
                BranchName = b.Session?.Branch?.BranchName,
                GymName = b.Session?.Branch?.Gym?.GymName,
                StartTime = b.Session?.StartTime ?? DateTime.MinValue,
                EndTime = b.Session?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }

        public async Task<IEnumerable<ClassBookingResponse>> GetPartnerClassBookingsAsync(Guid ownerId)
        {
            var bookings = await _bookingRepo.GetClassBookingsByOwnerIdAsync(ownerId);
            return bookings.Select(b => new ClassBookingResponse
            {
                BookingId = b.BookingId,
                ClassId = b.ClassId,
                ClassName = b.Class?.ClassName,
                CoachName = b.Class?.CoachName,
                BranchName = b.Class?.Branch?.BranchName,
                GymName = b.Class?.Branch?.Gym?.GymName,
                StartTime = b.Class?.StartTime ?? DateTime.MinValue,
                EndTime = b.Class?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }

        public async Task<IEnumerable<StaffCheckInBookingResponse>> GetStaffCheckInBookingsAsync(Guid staffId)
        {
            // Lưu ý: EF Core DbContext KHÔNG thread-safe → phải await tuần tự
            var gymBookings = await _bookingRepo.GetStaffGymBookingsForCheckInAsync(staffId);
            var classBookings = await _bookingRepo.GetStaffClassBookingsForCheckInAsync(staffId);

            var gymResponses = gymBookings.Select(gb => new StaffCheckInBookingResponse
            {
                BookingId = gb.BookingId,
                BookingCode = gb.BookingCode,
                BookingType = "GYM",
                UserId = gb.UserId,
                UserEmail = gb.User?.Email ?? "",
                UserFullName = gb.User?.FullName ?? "",
                SessionId = gb.SessionId,
                SessionName = gb.Session?.SessionName,
                BranchId = gb.Session?.BranchId ?? Guid.Empty,
                BranchName = gb.Session?.Branch?.BranchName,
                GymName = gb.Session?.Branch?.Gym?.GymName,
                StartTime = gb.Session?.StartTime ?? DateTime.MinValue,
                EndTime = gb.Session?.EndTime ?? DateTime.MinValue,
                Status = gb.Status,
                CheckInStatus = gb.CheckInStatus,
                CreditUsed = gb.CreditUsed,
                BookedAt = gb.BookedAt,
                QrToken = gb.QrToken
            });

            var classResponses = classBookings.Select(cb => new StaffCheckInBookingResponse
            {
                BookingId = cb.BookingId,
                BookingCode = cb.BookingCode,
                BookingType = "CLASS",
                UserId = cb.UserId,
                UserEmail = cb.User?.Email ?? "",
                UserFullName = cb.User?.FullName ?? "",
                ClassId = cb.ClassId,
                ClassName = cb.Class?.ClassName,
                CoachName = cb.Class?.CoachName,
                BranchId = cb.Class?.BranchId ?? Guid.Empty,
                BranchName = cb.Class?.Branch?.BranchName,
                GymName = cb.Class?.Branch?.Gym?.GymName,
                StartTime = cb.Class?.StartTime ?? DateTime.MinValue,
                EndTime = cb.Class?.EndTime ?? DateTime.MinValue,
                Status = cb.Status,
                CheckInStatus = cb.CheckInStatus,
                CreditUsed = cb.CreditUsed,
                BookedAt = cb.BookedAt,
                QrToken = cb.QrToken
            });

            var result = gymResponses
                .Concat(classResponses)
                .OrderByDescending(b => b.BookedAt)
                .ToList();

            return result;
        }
    }
}
