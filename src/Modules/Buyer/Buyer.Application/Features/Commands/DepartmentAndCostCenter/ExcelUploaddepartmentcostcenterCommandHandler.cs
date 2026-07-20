using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.DepartmentAndCostCenter
{
    public class UploadDepartmentCostCenterCommandHandler
        : IRequestHandler<UploadDepartmentCostCenterCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;

        public UploadDepartmentCostCenterCommandHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
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
                buyerId = request.UploadDepartmentCostCenterDto.BuyerId.Value;
            }
            else
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindByCondition(x => x.OrganizationId == request.UploadDepartmentCostCenterDto.OrganizationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyer == null)
                    throw new NotFoundCustomException("Buyer not found.", "");

                buyerId = buyer.Id;
            }

            // Check File
            if (request.UploadDepartmentCostCenterDto.File == null ||
                request.UploadDepartmentCostCenterDto.File.Length == 0)
            {
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
                var departmentName = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(departmentName))
                    continue;

                var department = await _repository.BuyerDepartment
                    .FindByCondition(x =>
                        x.BuyerId == buyerId &&
                        x.Department == departmentName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (department == null)
                {
                    department = new BuyerDepartment
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyerId,
                        Department = departmentName
                    };

                    await _repository.BuyerDepartment.CreateAsync(department);
                }

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
                var departmentName = row.Cell(1).GetString().Trim();
                var costCenterName = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(departmentName) ||
                    string.IsNullOrWhiteSpace(costCenterName))
                    continue;

                // Department must exist in Sheet1
                if (!departmentDictionary.ContainsKey(departmentName))
                    continue;

                var departmentId = departmentDictionary[departmentName];

                // Check duplicate Cost Center
                var costCenter = await _repository.BuyerCostCenter
                    .FindByCondition(x =>
                        x.DepartmentId == departmentId &&
                        x.CostCenter == costCenterName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (costCenter == null)
                {
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