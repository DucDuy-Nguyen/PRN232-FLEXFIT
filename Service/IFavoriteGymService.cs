using Flexfit.DTOs; // 👈 Nhớ thêm namespace chứa FavoriteGymResponse
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IFavoriteGymService
    {
        // Trả về chuỗi thông báo kết quả hành động (Thêm thành công / Hủy thành công)
        Task<string> ToggleFavoriteGymAsync(Guid userId, Guid gymId);

        // 👈 SỬA TẠI ĐÂY: Đổi từ FavoriteGym thành FavoriteGymResponse
        Task<IEnumerable<FavoriteGymResponse>> GetMyFavoriteGymsAsync(Guid userId);
    }
}