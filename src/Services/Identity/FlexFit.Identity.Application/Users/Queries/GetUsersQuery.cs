using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Users.Queries;

public sealed record PaginatedList<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    bool? IsEmailVerified = null,
    string? RoleName = null,
    string SortBy = "CreatedAt",
    string SortDirection = "desc") : IRequest<PaginatedList<UserDto>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var ascending = !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        // Fetch database paginated list directly using new repository method
        var (users, totalCount) = await _userRepository.GetPagedAsync(
            page,
            pageSize,
            request.Search,
            request.IsActive,
            request.IsEmailVerified,
            request.RoleName,
            request.SortBy,
            ascending,
            cancellationToken);

        var items = users.Select(user => new UserDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsEmailVerified,
            user.IsActive,
            user.AvatarUrl,
            user.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt))
            .ToList();

        return new PaginatedList<UserDto>(items, totalCount, page, pageSize);
    }
}
