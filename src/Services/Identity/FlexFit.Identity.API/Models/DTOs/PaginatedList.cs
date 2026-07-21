using System.Collections.Generic;

namespace FlexFit.Identity.API.Models.DTOs;

public sealed record PaginatedList<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
