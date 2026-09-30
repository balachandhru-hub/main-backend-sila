using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.CheckRFQUserAccess
{
    public class CheckRFQUserAccessQueryHandler
        : IRequestHandler<CheckRFQUserAccessQuery, bool>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public CheckRFQUserAccessQueryHandler(
            IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task<bool> Handle(
            CheckRFQUserAccessQuery request,
            CancellationToken cancellationToken)
        {
            var mapping = await _repositoryWrapper.RFQOrganizationUserMapping
                .FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.UserId == request.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (mapping == null)
            {
                throw new ForBiddenCustomException(
                    "Access denied.",
                    "You are not authorized to submit a quotation for this RFQ.");
            }

            return true;
        }
    }
}