using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DepartmentAndCostCenter
{
    public class UploadDepartmentCostCenterCommandHandler
        : IRequestHandler<UploadDepartmentCostCenterCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
         private readonly ILoggerManager _logger;

        public UploadDepartmentCostCenterCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UploadDepartmentCostCenterCommand request,
            CancellationToken cancellationToken)
        {
            Guid buyerId;

            // Get BuyerId
            if (request.UploadDepartmentCostCenterDto.BuyerId.HasValue &&
                request.UploadDepartmentCostCenterDto.BuyerId.Value != Guid.Empty)
            {
                _logger.LogInfo($"Using provided BuyerId: {request.UploadDepartmentCostCenterDto.BuyerId.Value}");
                buyerId = request.UploadDepartmentCostCenterDto.BuyerId.Value;
            }
            else
            {
                _logger.LogInfo($"Fetching BuyerId for OrganizationId: {request.UploadDepartmentCostCenterDto.OrganizationId}");
                var buyer = await _repository.BuyerBusinessProfile
                    .FindByCondition(x => x.OrganizationId == request.UploadDepartmentCostCenterDto.OrganizationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyer == null)
                {
                    _logger.LogError($"Buyer with OrganizationId {request.UploadDepartmentCostCenterDto.OrganizationId} not found.");
                    throw new NotFoundCustomException("Buyer not found.", "");
                }
                buyerId = buyer.Id;
            }

            // Check File
            if (request.UploadDepartmentCostCenterDto.File == null ||
                request.UploadDepartmentCostCenterDto.File.Length == 0)
            {
                _logger.LogError("No file uploaded or file is empty.");
                throw new BadRequestCustomException("Please upload an Excel file.", "");
            }

            // Read Excel
            using var stream = new MemoryStream();

            await request.UploadDepartmentCostCenterDto.File.CopyToAsync(stream);

            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            // ===========================
            // Read Department Sheet
            // ===========================

            var departmentSheet = workbook.Worksheet(1);

            Dictionary<string, Guid> departmentDictionary = new Dictionary<string, Guid>();

            foreach (var row in departmentSheet.RowsUsed().Skip(1))
            {
                _logger.LogInfo($"Processing row {row.RowNumber()} in Department sheet.");
                var departmentName = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(departmentName))
                    continue;
                _logger.LogInfo($"Skipping empty department name in row {row.RowNumber()}.");
                var department = await _repository.BuyerDepartment
                    .FindByCondition(x =>
                        x.BuyerId == buyerId &&
                        x.Department == departmentName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (department == null)
                {
                    _logger.LogInfo($"Creating new department '{departmentName}' for BuyerId {buyerId}.");
                    department = new BuyerDepartment
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyerId,
                        Department = departmentName
                    };

                    await _repository.BuyerDepartment.CreateAsync(department);
                }
                _logger.LogInfo($"Department '{departmentName}' processed with Id {department.Id}.");
                departmentDictionary[departmentName] = department.Id;
            }

            // Save Departments first
            await _repository.SaveAsync();


            // ===========================
            // Read CostCenter Sheet
            // ===========================

            var costCenterSheet = workbook.Worksheet(2);

            foreach (var row in costCenterSheet.RowsUsed().Skip(1))
            {
                _logger.LogInfo($"Processing row {row.RowNumber()} in CostCenter sheet.");
                var departmentName = row.Cell(1).GetString().Trim();
                var costCenterName = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(departmentName) ||
                    string.IsNullOrWhiteSpace(costCenterName))
                    continue;

                // Department must exist in Sheet1
                if (!departmentDictionary.ContainsKey(departmentName))
                    continue;

                var departmentId = departmentDictionary[departmentName];
                _logger.LogInfo($"Processing CostCenter '{costCenterName}' for Department '{departmentName}' (Id: {departmentId}).");
                // Check duplicate Cost Center
                var costCenter = await _repository.BuyerCostCenter
                    .FindByCondition(x =>
                        x.DepartmentId == departmentId &&
                        x.CostCenter == costCenterName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (costCenter == null)
                {
                    _logger.LogInfo($"Creating new CostCenter '{costCenterName}' for DepartmentId {departmentId}.");
                    costCenter = new BuyerCostCenter
                    {
                        Id = Guid.NewGuid(),
                        DepartmentId = departmentId,
                        CostCenter = costCenterName
                    };

                    await _repository.BuyerCostCenter.CreateAsync(costCenter);
                }
            }

            // Save Cost Centers
            await _repository.SaveAsync();

            return true;
        }
    }
}