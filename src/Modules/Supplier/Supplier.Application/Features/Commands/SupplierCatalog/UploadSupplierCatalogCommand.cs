using MediatR;
using MediatR;
using Microsoft.AspNetCore.Http;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public record UploadSupplierCatalogCommand(
    IFormFile File,
    Guid OrganizationId
) : IRequest<ExcelUploadResultDto>;
}
