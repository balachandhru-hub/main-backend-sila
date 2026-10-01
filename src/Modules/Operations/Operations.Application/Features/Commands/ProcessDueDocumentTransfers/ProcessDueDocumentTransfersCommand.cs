using MediatR;

namespace Operations.Application.Features.Commands.ProcessDueDocumentTransfers
{
    /// <summary>
    /// Sent by the document transfer worker: uploads the due transfer jobs to their external destination.
    /// </summary>
    public class ProcessDueDocumentTransfersCommand : IRequest<int>
    {

    }
}
