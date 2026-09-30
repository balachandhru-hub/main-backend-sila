using MediatR;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public record DeletePredefinedMaterialCommand(Guid Id) : IRequest<bool>;
}