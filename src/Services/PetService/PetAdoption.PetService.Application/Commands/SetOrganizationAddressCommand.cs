using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.Application.Commands;

public record SetOrganizationAddressCommand(
    Guid OrgId,
    decimal Lat,
    decimal Lng,
    string Line1,
    string City,
    string Region,
    string Country,
    string PostalCode,
    string ReviewerOrgId,
    string ReviewerOrgRole) : IRequest<SetOrganizationAddressResponse>;

public record SetOrganizationAddressResponse(
    Guid OrgId,
    decimal Lat,
    decimal Lng,
    string City,
    string Country);

public class SetOrganizationAddressCommandHandler
    : IRequestHandler<SetOrganizationAddressCommand, SetOrganizationAddressResponse>
{
    private readonly IOrganizationRepository _orgRepository;

    public SetOrganizationAddressCommandHandler(IOrganizationRepository orgRepository)
    {
        _orgRepository = orgRepository;
    }

    public async Task<SetOrganizationAddressResponse> Handle(
        SetOrganizationAddressCommand command, CancellationToken ct)
    {
        // Verify caller is an admin of the target org
        Guid.TryParse(command.ReviewerOrgId, out var reviewerOrgGuid);
        OrgAuthorization.EnsureMember(command.OrgId, reviewerOrgGuid == Guid.Empty ? null : reviewerOrgGuid, command.ReviewerOrgRole);

        var org = await _orgRepository.GetByIdAsync(command.OrgId, ct)
                  ?? Organization.Create(command.OrgId);

        org.SetAddress(new Address(
            command.Lat, command.Lng,
            command.Line1, command.City,
            command.Region, command.Country, command.PostalCode));

        await _orgRepository.UpsertAsync(org, ct);

        return new SetOrganizationAddressResponse(
            command.OrgId, command.Lat, command.Lng,
            command.City, command.Country);
    }
}
