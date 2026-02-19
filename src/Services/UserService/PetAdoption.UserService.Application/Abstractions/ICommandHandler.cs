namespace PetAdoption.UserService.Application.Abstractions;

/// <summary>
/// Handler for commands that modify state
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
