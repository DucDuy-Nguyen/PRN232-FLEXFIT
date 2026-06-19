public class AssignStaffDto
{
    public Guid UserId { get; set; }     // ID của người sẽ làm nhân viên
    public Guid BranchId { get; set; }   // ID của Chi nhánh muốn gán vào

}

public class AssignStaffByEmailDto
{
    public string Email { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
}

