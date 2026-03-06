using System.Diagnostics.CodeAnalysis;

namespace ProposalService.Application.Exceptions;

[ExcludeFromCodeCoverage]
public sealed class ProposalConcurrencyException : Exception
{
    public ProposalConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
