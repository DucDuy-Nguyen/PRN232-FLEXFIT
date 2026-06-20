namespace Flexfit.DTOs
{
    public class GymAmenityDto
    {
        public Guid AmenityId { get; set; }
        public string AmenityName { get; set; } = null!;
    }

    public class UpdateBranchAmenitiesRequest
    {
        // Danh sách các ID tiện ích được chọn cho chi nhánh này
        public List<Guid> AmenityIds { get; set; } = new List<Guid>();
    }
}