namespace PetAdoption.PetService.Infrastructure.Mediator;

/*
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        foreach (var validator in validators)
            validator.Validate(request);
        return next();
    }
}

public interface IValidator<in TRequest>
{
    void Validate(TRequest request);
}

public class CompleteFilingValidator : IValidator<CompleteFilingCommand>
{
    public void Validate(CompleteFilingCommand request)
    {
        if (request.FilingId == Guid.Empty)
            throw new ArgumentException("Filing Id cannot be empty");
    }
}*/