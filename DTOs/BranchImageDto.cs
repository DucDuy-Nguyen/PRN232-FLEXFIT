namespace Flexfit.DTOs
{
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
        // Danh sách các hình ảnh mới muốn nạp hoặc thay thế cho chi nhánh
        public List<AddBranchImageRequest> Images { get; set; } = new List<AddBranchImageRequest>();
    }
}