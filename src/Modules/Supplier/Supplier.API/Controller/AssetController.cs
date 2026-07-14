using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.Controllers;
using SharedKernel.LoggerServices;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using MediatR;
using Supplier.Application.Features.Commands.Asset;
using Supplier.Application.Features.Queries.Asset.GetDocument;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Controller for managing assets.
    /// </summary>
    [ApiController]
    public class AssetController : BaseController
    {
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;


        /// <summary>
        /// Constructor for Asset Controller.
        /// </summary>
        /// <param name="logger">The logger manager.</param>
        public AssetController(ILoggerManager logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        /// <summary>
        /// Uploads a document.
        /// </summary>
        /// <param name="asset">The details of the document to be uploaded.</param>
        /// <response code="200">File Uploaded successfully</response>
        /// <response code="400">Invalid file.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPost]
        [Route("api/v1/supplier/asset")]
        [ApiAuthorization(Name = "ASSET_CREATE")]
        [SwaggerOperation("UploadDocuments")]
        [SwaggerResponse(statusCode: 201, "File Uploaded successfully", typeof(SuccessResponseDto))]
        [SwaggerResponse(statusCode: 400, "Invalid file.", typeof(ErrorResponseDto))]
        [SwaggerResponse(statusCode: 401, "Unauthorized", typeof(ErrorResponseDto))]
        public IActionResult UploadDocument([FromBody] AssetUploadDto asset)
        {
            Guid userId = GetUserId();
            _logger.LogDebug($"Uploading documents for user with Id : {userId}");
            Guid result = _mediator.Send(new UploadAssetCommand(asset)).Result;
            _logger.LogDebug($"Document uploaded successfully for the file with Id: {result}");
            return StatusCode(201, new SuccessResponseDto { StatusCode = 201, Message = "Document Uploaded successfully.", Id = result.ToString() });
        }

        /// <summary>
        /// Downloads a document.
        /// </summary>
        /// <param name="assetId">The ID of the document to be downloaded.</param>
        /// <response code="200">File downloaded successfully</response>
        /// <response code="404">File not found.</response>
        /// <response code="500">Internal server error.</response>
        [HttpGet]
        [Route("api/v1/supplier/asset/{assetId}")]
        [ApiAuthorization(Name = "ASSET_DOWNLOAD")]
        [SwaggerOperation("GetDocument")]
        [SwaggerResponse(statusCode: 200, "Fetched the File Details", typeof(AssetDownloadDto))]
        [SwaggerResponse(statusCode: 404, "Not Found", typeof(ErrorResponseDto))]
        [SwaggerResponse(statusCode: 401, "Unauthorized", typeof(ErrorResponseDto))]
        public async Task<IActionResult> GetDocument(Guid assetId)
        {
            _logger.LogInfo($"Retrieving document for Asset Id: {assetId}");

            var result = await _mediator.Send(new GetDocumentQuery(assetId));

            _logger.LogInfo($"Document retrieved successfully for Asset Id: {assetId}");

            return Ok(result);
        }
    }
}