using Buyer.Application.Features.Commands.CreateOutlet;
using Buyer.Application.Features.Commands.CreateWishlist;
using Buyer.Application.Features.Commands.DecideWishlist;
using Buyer.Application.Features.Commands.UpdateWishlist;
using Buyer.Application.Features.Queries.GetOutlets;
using Buyer.Application.Features.Queries.GetWishlist;
using Buyer.Application.Features.Queries.GetWishlists;
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
    public class WishlistController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public WishlistController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/v1/buyer/wishlist")]
        [ApiAuthorization(Name = "CREATE_WISHLIST")]
        [SwaggerOperation("CreateWishlist")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Create([FromBody] WishlistWriteDto request)
        {
            _logger.LogDebug($"Creating wishlist. Name: {request.WishlistName}");
            Guid id = await _mediator.Send(new CreateWishlistCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Wishlist created."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/wishlist/{wishlistId}")]
        [ApiAuthorization(Name = "UPDATE_WISHLIST")]
        [SwaggerOperation("UpdateWishlist")]
        public async Task<IActionResult> Update([FromRoute] Guid wishlistId, [FromBody] WishlistWriteDto request)
        {
            _logger.LogDebug($"Updating wishlist. WishlistId: {wishlistId}");
            await _mediator.Send(new UpdateWishlistCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId,
                Request = request
            });
            return Ok(new SuccessResponseDto
            {
                Id = wishlistId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Wishlist updated."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/wishlist/{wishlistId}")]
        [ApiAuthorization(Name = "GET_WISHLIST")]
        [SwaggerOperation("GetWishlist")]
        public async Task<IActionResult> Get([FromRoute] Guid wishlistId)
        {
            _logger.LogDebug($"Fetching wishlist. WishlistId: {wishlistId}");
            WishlistResponseDto result = await _mediator.Send(new GetWishlistQuery
            {
                OrganizationId = GetOrganizationId(),
                WishlistId = wishlistId
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/wishlist")]
        [ApiAuthorization(Name = "GET_WISHLIST")]
        [SwaggerOperation("GetWishlists")]
        public async Task<IActionResult> List([FromQuery] int index = 0, [FromQuery] int limit = 20)
        {
            List<WishlistListItemDto> result = await _mediator.Send(new GetWishlistsQuery
            {
                OrganizationId = GetOrganizationId(),
                Index = index,
                Limit = limit
            });
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/wishlist/approval/{wishlistId}")]
        [ApiAuthorization(Name = "APPROVE_WISHLIST")]
        [SwaggerOperation("ApproveOrRejectWishlist")]
        public async Task<IActionResult> Decide([FromRoute] Guid wishlistId, [FromBody] WishlistDecisionDto decision)
        {
            _logger.LogDebug($"Processing wishlist approval. WishlistId: {wishlistId}");
            await _mediator.Send(new DecideWishlistCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId,
                Decision = decision
            });
            return Ok(new SuccessResponseDto
            {
                Id = wishlistId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Wishlist approval updated."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/outlets")]
        [ApiAuthorization(Name = "GET_OUTLET")]
        [SwaggerOperation("GetOutlets")]
        public async Task<IActionResult> Outlets()
        {
            List<OutletResponseDto> result = await _mediator.Send(new GetOutletsQuery
            {
                OrganizationId = GetOrganizationId()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/outlets")]
        [ApiAuthorization(Name = "CREATE_OUTLET")]
        [SwaggerOperation("CreateOutlet")]
        public async Task<IActionResult> CreateOutlet([FromBody] OutletWriteDto request)
        {
            Guid id = await _mediator.Send(new CreateOutletCommand
            {
                OrganizationId = GetOrganizationId(),
                Request = request
            });
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Outlet created."
            });
        }

    }
}
