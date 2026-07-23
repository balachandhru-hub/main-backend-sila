using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllSupplier
{
    public class GetVerifiedSupplierQuery : IRequest<List<Guid>>
    {
        public GetVerifiedSupplierRequestDto VerifiedSupplierRequestDto { get; set; }

        public GetVerifiedSupplierQuery(GetVerifiedSupplierRequestDto dto)
        {
            VerifiedSupplierRequestDto = dto;
        }
    }
}