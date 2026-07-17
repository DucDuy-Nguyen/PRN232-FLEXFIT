using System.Text;
using FlexFit.Engagement.Application.DTOs.AI;

namespace FlexFit.Engagement.Infrastructure.Services.AI;

public static class PromptBuilder
{
    public static string BuildPrompt(AIUserContextDto context, string userQuestion)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are FlexFit AI Assistant. You are a professional health, workout, and system assistant for the FlexFit system.");
        sb.AppendLine("You must ONLY answer using the provided actual FlexFit system data below.");
        sb.AppendLine("If information is unavailable or not in the user context, answer exactly: \"Tôi không tìm thấy thông tin này trong hệ thống FlexFit.\"");
        sb.AppendLine("Always answer in Vietnamese and format your response beautifully with Markdown.");
        sb.AppendLine("Never invent gyms, bookings, credits, classes, or profile information that are not in the context.");
        sb.AppendLine();

        sb.AppendLine("--- USER SYSTEM CONTEXT ---");
        sb.AppendLine($"User Name: {context.FullName}");
        sb.AppendLine($"Role: {context.Role}");
        sb.AppendLine($"Email: {context.Email}");

        if (context.Role.Contains("Member", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"Current Credits: {context.Credits} credits");
            sb.AppendLine($"Fitness Goal: {(string.IsNullOrEmpty(context.FitnessGoal) ? "Chưa cập nhật" : context.FitnessGoal)}");
            sb.AppendLine($"Height: {(context.Height.HasValue ? context.Height.Value + " cm" : "Chưa cập nhật")}");
            sb.AppendLine($"Weight: {(context.Weight.HasValue ? context.Weight.Value + " kg" : "Chưa cập nhật")}");

            sb.AppendLine("Favorite Gyms:");
            if (context.FavoriteGyms.Any()) { foreach (var g in context.FavoriteGyms) sb.AppendLine($"- {g}"); }
            else sb.AppendLine("- Chưa có phòng tập yêu thích.");

            sb.AppendLine("Favorite Classes:");
            if (context.FavoriteClasses.Any()) { foreach (var c in context.FavoriteClasses) sb.AppendLine($"- {c}"); }
            else sb.AppendLine("- Chưa có lớp học yêu thích.");

            sb.AppendLine("Recent Bookings (Gym and Classes):");
            if (context.RecentBookings.Any()) { foreach (var b in context.RecentBookings) sb.AppendLine($"- {b}"); }
            else sb.AppendLine("- Chưa có lịch đặt chỗ nào.");

            sb.AppendLine("Recent Workout History:");
            if (context.WorkoutHistorySummary.Any()) { foreach (var w in context.WorkoutHistorySummary) sb.AppendLine($"- {w}"); }
            else sb.AppendLine("- Chưa có lịch sử tập luyện.");
        }

        if (context.Role.Contains("Partner", StringComparison.OrdinalIgnoreCase) || context.Role.Contains("GymPartner", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("Owned Gyms:");
            if (context.OwnedGyms.Any()) { foreach (var gym in context.OwnedGyms) sb.AppendLine($"- {gym}"); }
            else sb.AppendLine("- Bạn chưa đăng ký sở hữu phòng tập nào.");
            sb.AppendLine($"Partner Summary: {context.PartnerSummary}");
        }

        if (context.Role.Contains("Staff", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("Managed Branches:");
            if (context.ManagedBranches.Any()) { foreach (var mb in context.ManagedBranches) sb.AppendLine($"- {mb}"); }
            else sb.AppendLine("- Bạn chưa được phân công quản lý chi nhánh nào.");
            sb.AppendLine($"Staff Working Summary: {context.StaffSummary}");
        }

        if (context.Role.Contains("Admin", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"Admin System Summary: {context.AdminSummary}");
        }

        sb.AppendLine("--- END OF CONTEXT ---");
        sb.AppendLine();
        sb.AppendLine($"User Question: {userQuestion}");
        sb.AppendLine("Response (Vietnamese):");

        return sb.ToString();
    }
}
