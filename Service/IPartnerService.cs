using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexfit.DTOs;
using Flexfit.DTOs.Review;

namespace Flexfit.Services
{
    public interface IPartnerService
    {
        Task<PartnerDashboardDto> GetDashboardStatsAsync(Guid ownerId);
        Task<IEnumerable<PartnerCustomerDto>> GetCustomersAsync(Guid ownerId);
        Task<IEnumerable<ReviewResponse>> GetReviewsAsync(Guid ownerId);
        Task<PartnerRevenueDto> GetRevenueAsync(Guid ownerId);
    }
}
