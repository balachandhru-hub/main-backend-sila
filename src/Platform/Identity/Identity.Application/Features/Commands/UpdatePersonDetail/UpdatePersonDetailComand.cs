using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Commands.UpdatePersonDetail
{
    public class UpdatePersonDetailCommand : IRequest<Unit>
    {
        public Guid PersonId { get; set; }

        public UpdatePersonDetailDto Data { get; set; } = new();
    }
}