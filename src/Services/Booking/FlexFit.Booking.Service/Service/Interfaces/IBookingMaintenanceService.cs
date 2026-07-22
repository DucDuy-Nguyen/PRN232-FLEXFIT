using System.Threading.Tasks;

namespace FlexFit.Booking.Service.Service.Interfaces
{
    public interface IBookingMaintenanceService
    {
        Task ProcessExpirationsAsync();
    }
}
