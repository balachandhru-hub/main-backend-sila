using MediatR;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetAllSuppliersQueryHandler
        : IRequestHandler<GetAllSuppliersQuery, List<GetAllSupplierDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetAllSuppliersQueryHandler(IRepositoryWrapper repositoryWrapper, ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }


        public async Task<List<GetAllSupplierDto>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching all suppliers with filters - OrganizationName: {request.OrganizationName}, Status: {request.Status}, Index: {request.Index}, Limit: {request.Limit}");
            var query = _repositoryWrapper.SupplierBusinessProfile
                .FindByCondition(x => x.IsActive);


            if (!string.IsNullOrWhiteSpace(request.OrganizationName))
            {
                query = query.Where(x =>
                    x.OrganizationName.Contains(request.OrganizationName));
            }


            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x =>
                    x.Status == request.Status);
            }

            var suppliers = query
                .OrderByDescending(x => x.DateUpdated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            var result = new List<GetAllSupplierDto>();



            foreach (var supplier in suppliers)
            {
                var dto = new GetAllSupplierDto{
                
                    OrganizationId = supplier.OrganizationId,
                    SNID = supplier.SNID,

                    
                    
                        OrganizationName = supplier.OrganizationName,
                        Email = supplier.Email,
                        Phone = supplier.Phone,
                        Country = supplier.Country,                    
                        City = supplier.City,
                        State = supplier.State,
                        Industry = supplier.Industry,
                        BusinessType = supplier.BusinessType,                       
                        YearEstablished = supplier.YearEstablished,
                        Website = supplier.Website,
                        Description = supplier.Description,
                       
                    
                };


                result.Add(dto);
            }


            return result;
        }
    }
}