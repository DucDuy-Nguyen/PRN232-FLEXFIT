using System.Collections.Generic;

namespace FlexFit.Identity.Service.DTOs;

public sealed record PaginatedList<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
