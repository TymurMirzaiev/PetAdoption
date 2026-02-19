namespace PetAdoption.UserService.Application.Queries;

using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Exceptions;

public record GetUserByEmailQuery(string Email) : IQuery<UserDto>;

public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, UserDto>
{
    private readonly IUserQueryStore _userQueryStore;

    public GetUserByEmailQueryHandler(IUserQueryStore userQueryStore)
    {
        _userQueryStore = userQueryStore;
    }

    public async Task<UserDto> HandleAsync(
        GetUserByEmailQuery query,
        CancellationToken cancellationToken = default)
    {
        var email = Email.From(query.Email);
        var user = await _userQueryStore.GetByEmailAsync(email);

        if (user == null)
        {
            throw new UserNotFoundException(query.Email);
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
