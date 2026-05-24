
namespace Flexfit.Models
{
    public partial class FavoriteClass
    {
        public Guid UserId { get; set; }

        public Guid ClassId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual Class Class { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }
}