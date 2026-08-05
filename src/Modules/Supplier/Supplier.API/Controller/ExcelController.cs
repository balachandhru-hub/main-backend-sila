using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Supplier.Application.Features.Commands.SupplierCatalog;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using SharedKernel.Controllers;

namespace Supplier.API.Controllers
{
    [ApiController]
    public class ExcelController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ExcelController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Upload Supplier Catalog via Excel
        /// </summary>
        [HttpPost]
        [Route("api/v1/supplier/catalog-upload")]
        [Consumes("multipart/form-data")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPLOAD_SUPPLIER_CATALOG")]
        [SwaggerOperation("UploadSupplierCatalog")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Upload successful")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UploadSupplierCatalog([Required] IFormFile file)
        {
            _logger.LogDebug("Starting Supplier Catalog upload.");

            var result = await _mediator.Send(
                new UploadSupplierCatalogCommand(file, GetOrganizationId()));

            _logger.LogDebug($"Supplier Catalog upload completed. Records inserted: {result}");

            return Ok(new
            {
                Message = "Upload successful.",
                RecordsInserted = result
            });
        }
    }
}

