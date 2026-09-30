using MediatR;
using Microsoft.EntityFrameworkCore;
using Buyer.Infrastructure.Contracts.IRepository;

namespace Buyer.Application.Features.Queries.GetExternalSupplierByEmail
{
    public class GetExternalSupplierByEmailQueryHandler
        : IRequestHandler<GetExternalSupplierByEmailQuery, Guid?>
    {
        private readonly IRepositoryWrapper _repository;

        public GetExternalSupplierByEmailQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<Guid?> Handle(
            GetExternalSupplierByEmailQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return null;
            }

            var normalizedEmail = request.Email.Trim().ToLower();

            var externalSupplier = await _repository.ExternalSupplier
                .FindByCondition(x =>
                    x.IsActive &&
                    x.Email.ToLower() == normalizedEmail)
                .FirstOrDefaultAsync(cancellationToken);

            return externalSupplier?.Id;
        }
    }
}
