using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetPersonDetail
{
    public class GetPersonDetailQuery : IRequest<PersonDetailDto>
    {
        public Guid PersonId { get; set; }
    }
}