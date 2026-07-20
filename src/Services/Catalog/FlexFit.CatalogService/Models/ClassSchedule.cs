using System;

namespace FlexFit.CatalogService.Models;

public partial class ClassSchedule
{
    public Guid ScheduleId { get; set; }

    public Guid ClassId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly StartHour { get; set; }

    public TimeOnly EndHour { get; set; }

    public virtual Class Class { get; set; } = null!;
}
