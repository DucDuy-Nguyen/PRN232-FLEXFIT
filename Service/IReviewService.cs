using Flexfit.DTOs.Review;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IReviewService
    {
        Task<ReviewResponse> CreateBookingReviewAsync(Guid userId, CreateReviewRequest request);
        Task<IEnumerable<ReviewResponse>> GetGymReviewsAsync(Guid gymId);
    }
}
