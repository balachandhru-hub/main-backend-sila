using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.GetExternalSupplierName
{
    public class GetExternalSupplierNameQueryHandler
        : IRequestHandler<GetExternalSupplierNameQuery, ExternalSupplierNameDto>
    {
        private readonly IRepositoryWrapper _repository;

        public GetExternalSupplierNameQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<ExternalSupplierNameDto> Handle(
            GetExternalSupplierNameQuery request,
            CancellationToken cancellationToken)
        {
            var externalSupplier = await _repository.ExternalSupplier
                .FindByCondition(x => x.Id == request.ExternalSupplierId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (externalSupplier == null)
            {
                throw new NotFoundCustomException(
                    "External supplier not found.",
                    $"No external supplier exists with Id: {request.ExternalSupplierId}.");
            }

            return new ExternalSupplierNameDto
            {
                ExternalSupplierId = externalSupplier.Id,
                ExternalSupplierName = externalSupplier.SupplierName,
                SupplierType = Common.EXTERNAL_SUPPLIER
            };
        }
    }
}
