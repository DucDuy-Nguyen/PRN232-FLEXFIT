namespace Flexfit.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public List<string> Roles { get; set; } = new();

    }
}
