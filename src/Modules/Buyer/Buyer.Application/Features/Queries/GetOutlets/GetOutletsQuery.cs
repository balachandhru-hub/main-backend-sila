using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetOutlets
{
    public class GetOutletsQuery : IRequest<List<OutletResponseDto>>
    {
        public Guid OrganizationId { get; set; }
    }
}
