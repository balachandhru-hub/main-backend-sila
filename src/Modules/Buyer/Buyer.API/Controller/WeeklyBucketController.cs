using Buyer.Application.Features.Commands.AddWeeklyBucketItems;
using Buyer.Application.Features.Commands.DecideWeeklyBucket;
using Buyer.Application.Features.Commands.DecideWeeklyBucketRecommendation;
using Buyer.Application.Features.Commands.FreezeWeeklyBucket;
using Buyer.Application.Features.Commands.RefreshWeeklyBucketInventory;
using Buyer.Application.Features.Commands.RemoveWeeklyBucketItem;
using Buyer.Application.Features.Commands.RetryWeeklyBucketPurchaseOrders;
using Buyer.Application.Features.Commands.SetWeeklyBucketItemApprovedQuantity;
using Buyer.Application.Features.Commands.UpdateWeeklyBucketItemQuantity;
using Buyer.Application.Features.Commands.UpsertCatalogMaterialMapping;
using Buyer.Application.Features.Queries.GetCurrentWeeklyBucket;
using Buyer.Application.Features.Queries.GetWeeklyBucket;
using Buyer.Application.Features.Queries.GetWeeklyBuckets;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    [ApiController]
    public class WeeklyBucketController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public WeeklyBucketController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/weekly-buckets/current")]
        [ApiAuthorization(Name = "GET_WEEKLY_BUCKET")]
        [SwaggerOperation("GetCurrentWeeklyBucket")]
        [SwaggerResponse(200, type: typeof(WeeklyBucketDetailDto))]
        public async Task<IActionResult> GetCurrent([FromQuery] Guid? outletId)
        {
            _logger.LogDebug($"Fetching the active weekly bucket. OutletId: {outletId}");
            WeeklyBucketDetailDto result = await _mediator.Send(new GetCurrentWeeklyBucketQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                OutletId = outletId
            });
            _logger.LogDebug($"Active weekly bucket fetched. BucketCode: {result.BucketCode}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/weekly-buckets")]
        [ApiAuthorization(Name = "GET_WEEKLY_BUCKET")]
        [SwaggerOperation("GetWeeklyBuckets")]
        [SwaggerResponse(200, type: typeof(List<WeeklyBucketListItemDto>))]
        public async Task<IActionResult> List([FromQuery] int index = 0, [FromQuery] int limit = 20)
        {
            _logger.LogDebug($"Fetching weekly buckets. Index: {index}, Limit: {limit}");
            List<WeeklyBucketListItemDto> result = await _mediator.Send(new GetWeeklyBucketsQuery
            {
                OrganizationId = GetOrganizationId(),
                Index = index,
                Limit = limit
            });
            _logger.LogDebug($"Weekly buckets fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}")]
        [ApiAuthorization(Name = "GET_WEEKLY_BUCKET")]
        [SwaggerOperation("GetWeeklyBucket")]
        [SwaggerResponse(200, type: typeof(WeeklyBucketDetailDto))]
        public async Task<IActionResult> Get([FromRoute] Guid bucketId)
        {
            _logger.LogDebug($"Fetching weekly bucket. WeeklyBucketId: {bucketId}");
            WeeklyBucketDetailDto result = await _mediator.Send(new GetWeeklyBucketQuery
            {
                OrganizationId = GetOrganizationId(),
                WeeklyBucketId = bucketId
            });
            _logger.LogDebug($"Weekly bucket fetched. WeeklyBucketId: {bucketId}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/weekly-buckets/items")]
        [ApiAuthorization(Name = "ADD_WEEKLY_BUCKET_ITEM")]
        [SwaggerOperation("AddWeeklyBucketItems")]
        [SwaggerResponse(200, type: typeof(WeeklyBucketAddItemsResultDto))]
        public async Task<IActionResult> AddItems([FromBody] WeeklyBucketItemsWriteDto request)
        {
            _logger.LogDebug($"Adding items to the weekly bucket. OutletId: {request.OutletId}, Products: {request.Items?.Count ?? 0}");
            WeeklyBucketAddItemsResultDto result = await _mediator.Send(new AddWeeklyBucketItemsCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Items added to the weekly bucket. WeeklyBucketId: {result.BucketId}, BucketCode: {result.BucketCode}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}/items/{itemId}")]
        [ApiAuthorization(Name = "ADD_WEEKLY_BUCKET_ITEM")]
        [SwaggerOperation("UpdateWeeklyBucketItemQuantity")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> UpdateItemQuantity([FromRoute] Guid bucketId, [FromRoute] Guid itemId, [FromBody] QuantityWriteDto request)
        {
            _logger.LogDebug($"Changing requested quantity. WeeklyBucketId: {bucketId}, ItemId: {itemId}");
            await _mediator.Send(new UpdateWeeklyBucketItemQuantityCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WeeklyBucketId = bucketId,
                ItemId = itemId,
                Request = request
            });
            _logger.LogDebug($"Requested quantity changed. WeeklyBucketId: {bucketId}, ItemId: {itemId}");
            return Ok(new SuccessResponseDto
            {
                Id = itemId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Requested quantity updated."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}/items/{itemId}/approved-quantity")]
        [ApiAuthorization(Name = "REVIEW_WEEKLY_BUCKET")]
        [SwaggerOperation("SetWeeklyBucketItemApprovedQuantity")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> SetApprovedQuantity([FromRoute] Guid bucketId, [FromRoute] Guid itemId, [FromBody] QuantityWriteDto request)
        {
            _logger.LogDebug($"Setting final quantity. WeeklyBucketId: {bucketId}, ItemId: {itemId}");
            await _mediator.Send(new SetWeeklyBucketItemApprovedQuantityCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WeeklyBucketId = bucketId,
                ItemId = itemId,
                Request = request
            });
            _logger.LogDebug($"Final quantity set. WeeklyBucketId: {bucketId}, ItemId: {itemId}");
            return Ok(new SuccessResponseDto
            {
                Id = itemId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Final quantity updated."
            });
        }

        [HttpDelete]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}/items/{itemId}")]
        [ApiAuthorization(Name = "ADD_WEEKLY_BUCKET_ITEM")]
        [SwaggerOperation("RemoveWeeklyBucketItem")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> RemoveItem([FromRoute] Guid bucketId, [FromRoute] Guid itemId)
        {
            _logger.LogDebug($"Removing weekly bucket line. WeeklyBucketId: {bucketId}, ItemId: {itemId}");
            await _mediator.Send(new RemoveWeeklyBucketItemCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                WeeklyBucketId = bucketId,
                ItemId = itemId
            });
            _logger.LogDebug($"Weekly bucket line removed. WeeklyBucketId: {bucketId}, ItemId: {itemId}");
            return Ok(new SuccessResponseDto
            {
                Id = itemId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Product removed from the weekly bucket."
            });
        }

        [HttpPost]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}/refresh-inventory")]
        [ApiAuthorization(Name = "ADD_WEEKLY_BUCKET_ITEM")]
        [SwaggerOperation("RefreshWeeklyBucketInventory")]
        [SwaggerResponse(200, type: typeof(WeeklyBucketDetailDto))]
        public async Task<IActionResult> RefreshInventory([FromRoute] Guid bucketId)
        {
            _logger.LogDebug($"Refreshing weekly bucket inventory. WeeklyBucketId: {bucketId}");
            WeeklyBucketDetailDto result = await _mediator.Send(new RefreshWeeklyBucketInventoryCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WeeklyBucketId = bucketId
            });
            _logger.LogDebug($"Weekly bucket inventory refreshed. WeeklyBucketId: {bucketId}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}/recommendations/{recommendationId}")]
        [ApiAuthorization(Name = "DECIDE_WEEKLY_BUCKET_RECOMMENDATION")]
        [SwaggerOperation("DecideWeeklyBucketRecommendation")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> DecideRecommendation(
            [FromRoute] Guid bucketId,
            [FromRoute] Guid recommendationId,
            [FromBody] WeeklyBucketRecommendationDecisionDto decision)
        {
            _logger.LogDebug($"Deciding recommendation. WeeklyBucketId: {bucketId}, RecommendationId: {recommendationId}");
            await _mediator.Send(new DecideWeeklyBucketRecommendationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WeeklyBucketId = bucketId,
                RecommendationId = recommendationId,
                Decision = decision
            });
            _logger.LogDebug($"Recommendation decided. WeeklyBucketId: {bucketId}, RecommendationId: {recommendationId}");
            return Ok(new SuccessResponseDto
            {
                Id = recommendationId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Recommendation updated."
            });
        }

        [HttpPost]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}/freeze")]
        [ApiAuthorization(Name = "FREEZE_WEEKLY_BUCKET")]
        [SwaggerOperation("FreezeWeeklyBucket")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Freeze([FromRoute] Guid bucketId)
        {
            _logger.LogDebug($"Freezing weekly bucket. WeeklyBucketId: {bucketId}");
            await _mediator.Send(new FreezeWeeklyBucketCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WeeklyBucketId = bucketId
            });
            _logger.LogDebug($"Weekly bucket frozen. WeeklyBucketId: {bucketId}");
            return Ok(new SuccessResponseDto
            {
                Id = bucketId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Weekly bucket frozen and sent for approval."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/weekly-buckets/approval/{bucketId}")]
        [ApiAuthorization(Name = "APPROVE_WEEKLY_BUCKET")]
        [SwaggerOperation("ApproveOrRejectWeeklyBucket")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Decide([FromRoute] Guid bucketId, [FromBody] WeeklyBucketDecisionDto decision)
        {
            _logger.LogDebug($"Processing weekly bucket approval. WeeklyBucketId: {bucketId}");
            await _mediator.Send(new DecideWeeklyBucketCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WeeklyBucketId = bucketId,
                Decision = decision
            });
            _logger.LogDebug($"Weekly bucket approval processed. WeeklyBucketId: {bucketId}");
            return Ok(new SuccessResponseDto
            {
                Id = bucketId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Weekly bucket approval updated."
            });
        }

        [HttpPost]
        [Route("api/v1/buyer/weekly-buckets/{bucketId}/retry-purchase-orders")]
        [ApiAuthorization(Name = "FREEZE_WEEKLY_BUCKET")]
        [SwaggerOperation("RetryWeeklyBucketPurchaseOrders")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> RetryPurchaseOrders([FromRoute] Guid bucketId)
        {
            _logger.LogDebug($"Retrying weekly bucket purchase orders. WeeklyBucketId: {bucketId}");
            await _mediator.Send(new RetryWeeklyBucketPurchaseOrdersCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WeeklyBucketId = bucketId
            });
            _logger.LogDebug($"Weekly bucket purchase orders retried. WeeklyBucketId: {bucketId}");
            return Ok(new SuccessResponseDto
            {
                Id = bucketId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Purchase orders created."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/catalog-material-mapping")]
        [ApiAuthorization(Name = "MANAGE_CATALOG_MATERIAL_MAPPING")]
        [SwaggerOperation("UpsertCatalogMaterialMapping")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> UpsertCatalogMaterialMapping([FromBody] CatalogMaterialMappingWriteDto request)
        {
            _logger.LogDebug($"Saving catalog material mapping. CatalogId: {request.CatalogId}, MaterialId: {request.MaterialId}");
            Guid id = await _mediator.Send(new UpsertCatalogMaterialMappingCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Catalog material mapping saved. MappingId: {id}");
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Material mapping saved."
            });
        }
    }
}
