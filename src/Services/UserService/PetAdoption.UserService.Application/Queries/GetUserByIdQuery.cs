namespace PetAdoption.UserService.Application.Queries;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

public record GetUserByIdQuery(string UserId) : IQuery<UserDto>;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserQueryStore _userQueryStore;

    public GetUserByIdQueryHandler(IUserQueryStore userQueryStore)
    {
        _userQueryStore = userQueryStore;
    }

    public async Task<UserDto> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = UserId.From(query.UserId);
        var user = await _userQueryStore.GetByIdAsync(userId);

        if (user == null)
        {
            throw new UserNotFoundException(query.UserId);
        }

        return new UserDto(
            user.Id.Value,
            user.Email.Value,
            user.FullName.Value,
            user.PhoneNumber?.Value,
            user.Status.ToString(),
            user.Role.ToString(),
            new UserPreferencesDto(
                user.Preferences.PreferredPetType,
                user.Preferences.PreferredSizes,
                user.Preferences.PreferredAgeRange,
                user.Preferences.ReceiveEmailNotifications,
                user.Preferences.ReceiveSmsNotifications
            ),
            user.RegisteredAt,
            user.UpdatedAt,
            user.LastLoginAt
        );
    }
}
