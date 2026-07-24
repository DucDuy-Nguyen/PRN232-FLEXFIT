using System;
using System.Collections.Generic;

namespace FlexFit.Catalog.Service.DTOs;

public class BranchDto
{
    public Guid BranchId { get; set; }
    public Guid GymId { get; set; }
    public string BranchName { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int CreditCost { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<StaffInfoDto> Staffs { get; set; } = new List<StaffInfoDto>();
    public List<GymAmenityDto> Amenities { get; set; } = new List<GymAmenityDto>();
    public List<BranchImageDto> Images { get; set; } = new List<BranchImageDto>();
}

public class StaffInfoDto
{
    public Guid StaffId { get; set; }
    public string FullName { get; set; } = null!;
}

public class CreateBranchRequest
{
    public Guid GymId { get; set; }
    public string BranchName { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int CreditCost { get; set; }
}

public class UpdateBranchRequest
{
    public string BranchName { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int CreditCost { get; set; }
}

public class BranchImageDto
{
    public Guid BranchImageId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public int DisplayOrder { get; set; }
}

public class AddBranchImageRequest
{
    public string ImageUrl { get; set; } = null!;
    public int DisplayOrder { get; set; }
}

public class UpdateBranchImagesRequest
{
    public List<AddBranchImageRequest> Images { get; set; } = new List<AddBranchImageRequest>();
}

public class AssignStaffDto
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
}

public class AssignStaffByEmailDto
{
    public string Email { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
}

public class UpdateBranchStaffDto
{
    public Guid BranchId { get; set; }
    public Guid NewStaffId { get; set; }
}

public class GymAmenityDto
{
    public Guid AmenityId { get; set; }
    public string AmenityName { get; set; } = null!;
}

public class UpdateBranchAmenitiesRequest
{
    public List<Guid> AmenityIds { get; set; } = new List<Guid>();
}

