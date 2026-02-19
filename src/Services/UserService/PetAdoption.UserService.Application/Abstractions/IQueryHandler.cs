namespace PetAdoption.UserService.Application.Abstractions;

/// <summary>
/// Handler for queries that read data
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
