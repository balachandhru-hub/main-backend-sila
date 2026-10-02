using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.LoggerServices;
using SharedKernel.Security;
using Supplier.Application.Features.Commands.SupplierCatalog;
using Supplier.Domain.Dto;

namespace Supplier.API.Controllers
{
    /// <summary>
    /// Catalog updates that come from the integration service after it has read a supplier's own
    /// API. Called service to service with the internal key, not by users.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class InternalCatalogController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public InternalCatalogController(
            IMediator mediator,
            ILoggerManager logger,
            IConfiguration configuration)
        {
            _mediator = mediator;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("api/v1/supplier/internal/catalog-sync")]
        [ValidateModelState]
        public async Task<IActionResult> SyncSupplierCatalog([FromBody] SupplierCatalogSyncRequestDto request)
        {
            InternalServiceKey.Require(Request, _configuration);

            _logger.LogDebug($"Syncing supplier catalog: {request.OrganizationId}");

            SupplierCatalogSyncResultDto result = await _mediator.Send(new SyncSupplierCatalogCommand
            {
                Request = request
            });

            _logger.LogDebug($"Supplier catalog synced successfully: {request.OrganizationId}");

            return Ok(result);
        }
    }
}
