namespace ProposalService.Application.Exceptions;

public sealed class ProposalConcurrencyException : Exception
{
    public ProposalConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
