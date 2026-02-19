namespace PetAdoption.UserService.Application.Queries;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Interfaces;

public record GetUsersQuery(int Skip = 0, int Take = 50) : IQuery<GetUsersResponse>;

public record GetUsersResponse(
    List<UserListItemDto> Users,
    int Total,
    int Skip,
    int Take
);

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, GetUsersResponse>
{
    private readonly IUserQueryStore _userQueryStore;

    public GetUsersQueryHandler(IUserQueryStore userQueryStore)
    {
        _userQueryStore = userQueryStore;
    }

    public async Task<GetUsersResponse> HandleAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var users = await _userQueryStore.GetAllAsync(query.Skip, query.Take);
        var total = await _userQueryStore.CountAsync();

        var userDtos = users.Select(user => new UserListItemDto(
            user.Id.Value,
            user.Email.Value,
            user.FullName.Value,
            user.Status.ToString(),
            user.Role.ToString(),
            user.RegisteredAt
        )).ToList();

        return new GetUsersResponse(
            userDtos,
            total,
            query.Skip,
            query.Take
        );
    }
}
