using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetProperties
{
    public class GetPropertiesQuery : IRequest<List<PropertyResponseDto>>
    {
        public Guid OrganizationId { get; set; }
    }
}
