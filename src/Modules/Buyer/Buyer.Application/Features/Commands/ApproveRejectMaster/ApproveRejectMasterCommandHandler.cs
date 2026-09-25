using Buyer.Application.Features.Queries.Asset.GetDocument;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ApproveRejectMaster
{
    public class ApproveRejectMasterCommandHandler
        : IRequestHandler<ApproveRejectMasterCommand, Guid>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;

        public ApproveRejectMasterCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMediator mediator)
        {
            _repositoryWrapper = repository;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Guid> Handle(
            ApproveRejectMasterCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Processing item master approval. " +
                $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                $"UserId: {request.UserId}");

            // ---------------------------------------------------------
            // 1. Validate Status
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(request.Approval.Status))
            {
                throw new BadRequestCustomException(
                    "Approval status is required.",
                    "Please provide APPROVE or REJECT.");
            }

            if (request.Approval.Status != Common.APPROVED &&
                request.Approval.Status != Common.REJECTED)
            {
                throw new BadRequestCustomException(
                    "Invalid approval status.",
                    "Status must be APPROVE or REJECT.");
            }

            // ---------------------------------------------------------
            // 2. Get Material Approval Flow Mapping
            //    (PredefinedMaterialId is polymorphic: it points to a
            //    PredefinedMaterial row for the manual flow, or to an
            //    ExcelMaterialMaster row - one per whole Excel upload -
            //    for the bulk Excel flow. UploadType tells us which.)
            // ---------------------------------------------------------

            var materialApprovalFlowMapping =
                await _repositoryWrapper
                    .ApprovalFlowPredefinedMaterialMapping
                    .FindFirstByConditionAsync(x =>
                        x.PredefinedMaterialId ==
                        request.PredefinedMaterialId &&
                        x.IsActive);

            if (materialApprovalFlowMapping == null)
            {
                _logger.LogError(
                    $"Approval flow mapping not found for predefined material. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}");
                throw new NotFoundCustomException(
                    "Approval flow mapping not found.",
                    "No approval flow is configured for this material.");
            }

            bool isExcel = materialApprovalFlowMapping.UploadType == Common.UPLOAD_TYPE_EXCEL;

            // ---------------------------------------------------------
            // 3. Get Predefined Material / Excel Material Batch
            // ---------------------------------------------------------

            PredefinedMaterial predefinedMaterial = null;
            ExcelMaterialMaster excelMaterialMaster = null;

            if (isExcel)
            {
                excelMaterialMaster =
                    await _repositoryWrapper.ExcelMaterialMaster
                        .FindFirstByConditionAsync(x =>
                            x.Id == request.PredefinedMaterialId &&
                            x.IsActive);

                if (excelMaterialMaster == null)
                {
                    _logger.LogError(
                        $"Excel material batch not found. " +
                        $"ExcelMaterialMasterId: {request.PredefinedMaterialId}");
                    throw new NotFoundCustomException(
                        "Excel material batch not found.",
                        $"No Excel material batch found for Id: {request.PredefinedMaterialId}");
                }
            }
            else
            {
                predefinedMaterial =
                    await _repositoryWrapper.PredefinedMaterial
                        .FindFirstByConditionAsync(x =>
                            x.Id == request.PredefinedMaterialId &&
                            x.IsActive);

                if (predefinedMaterial == null)
                {
                    _logger.LogError(
                        $"Predefined material not found. " +
                        $"PredefinedMaterialId: {request.PredefinedMaterialId}");
                    throw new NotFoundCustomException(
                        "Predefined material not found.",
                        $"No predefined material found for Id: {request.PredefinedMaterialId}");
                }
            }

            // ---------------------------------------------------------
            // 4. Get Material Specific Approval Users
            //    (One approval CHAIN either way - for Excel this is the
            //    single chain for the whole batch, not per row.)
            // ---------------------------------------------------------

            var approvalUsers =
                await _repositoryWrapper
                    .PredefinedMaterialApprovalFlowUserMapping
                    .FindByCondition(x =>
                        x.ApprovalFlowPredefinedMaterialId ==
                        materialApprovalFlowMapping.Id &&
                        x.IsActive)
                    .OrderBy(x => x.Order)
                    .ToListAsync(cancellationToken);

            if (!approvalUsers.Any())
            {
                _logger.LogError(
                    $"Approval users not found for predefined material. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}");
                throw new NotFoundCustomException(
                    "Approval users not found.",
                    "No approval users are configured for this material.");
            }

            // ---------------------------------------------------------
            // 5. Find Current Logged-In User
            // ---------------------------------------------------------

            var currentApproval =
                approvalUsers.FirstOrDefault(x =>
                    x.UserId == request.UserId);

            if (currentApproval == null)
            {
                _logger.LogError(
                    $"Current user is not part of the approval flow. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                    $"UserId: {request.UserId}");
                throw new ForBiddenCustomException(
                     "You are not authorized to approve this material.",
                     "The current user is not part of the approval flow.");
            }

            // ---------------------------------------------------------
            // 6. Check Whether Current User Already Approved/Rejected
            // ---------------------------------------------------------

            if (currentApproval.Status == Common.APPROVED ||
                currentApproval.Status == Common.REJECTED)
            {
                _logger.LogError(
                    $"Current user has already completed approval. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                    $"UserId: {request.UserId}, " +
                    $"Status: {currentApproval.Status}");
                throw new PreConditionFailedCustomException(
                    "Approval already completed.",
                    "You have already approved or rejected this material.");
            }

            // ---------------------------------------------------------
            // 7. Check Previous Approval Level
            // ---------------------------------------------------------

            var previousApprovals =
                approvalUsers
                    .Where(x => x.Order < currentApproval.Order)
                    .ToList();

            bool previousLevelsApproved =
                previousApprovals.All(x =>
                    x.Status == Common.APPROVED);

            if (!previousLevelsApproved)
            {
                _logger.LogError(
                    $"Previous approval level is pending. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                    $"UserId: {request.UserId}");
                throw new PreConditionFailedCustomException(
                    "Previous approval is pending.",
                    "The previous approval level must be approved first.");
            }

            // ---------------------------------------------------------
            // 8. Update Current Approval
            // ---------------------------------------------------------

            currentApproval.Status =
                request.Approval.Status;

            currentApproval.Comment =
                request.Approval.Comment;

            _repositoryWrapper
                .PredefinedMaterialApprovalFlowUserMapping
                .Update(currentApproval);

            // ---------------------------------------------------------
            // 9. Find Last Approval Level
            // ---------------------------------------------------------

            int lastApprovalOrder =
                approvalUsers.Max(x => x.Order);

            // ---------------------------------------------------------
            // 10. If Rejected
            // ---------------------------------------------------------

            if (request.Approval.Status == Common.REJECTED)
            {
                if (isExcel)
                {
                    // Same quirk as the manual flow: if the last approver
                    // is the one rejecting, the approval process itself is
                    // "done" (COMPLETE), just with nothing created.
                    excelMaterialMaster.Status =
                        currentApproval.Order == lastApprovalOrder
                            ? Common.COMPLETE
                            : Common.REJECTED;

                    _repositoryWrapper.ExcelMaterialMaster
                        .Update(excelMaterialMaster);

                    await _repositoryWrapper.SaveAsync();

                    _logger.LogInfo(
                        $"Excel material batch rejected. " +
                        $"ExcelMaterialMasterId: {excelMaterialMaster.Id}, " +
                        $"UserId: {request.UserId}, " +
                        $"Status: {excelMaterialMaster.Status}");

                    return excelMaterialMaster.Id;
                }

                // If the last approver rejects,
                // the approval process is completed.
                if (currentApproval.Order == lastApprovalOrder)
                {
                    predefinedMaterial.Status = Common.COMPLETE;
                }
                else
                {
                    // Keep existing logic for rejection
                    predefinedMaterial.Status = Common.REJECTED;
                }

                _repositoryWrapper
                    .PredefinedMaterial
                    .Update(predefinedMaterial);

                await _repositoryWrapper.SaveAsync();

                _logger.LogInfo(
                    $"Predefined material rejected. " +
                    $"PredefinedMaterialId: {predefinedMaterial.Id}, " +
                    $"UserId: {request.UserId}, " +
                    $"MaterialStatus: {predefinedMaterial.Status}");

                return predefinedMaterial.Id;
            }

            // ---------------------------------------------------------
            // 11. If Current User Is Last Approver
            // ---------------------------------------------------------

            if (currentApproval.Order == lastApprovalOrder)
            {
                // -----------------------------------------------------
                // 12. Verify All Approval Levels Are Approved
                // -----------------------------------------------------

                bool allApproved =
                    approvalUsers.All(x =>
                        x.Status == Common.APPROVED);

                if (!allApproved)
                {
                    _logger.LogError(
                        $"Not all approval levels are approved. " +
                        $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                        $"UserId: {request.UserId}");

                    throw new PreConditionFailedCustomException(
                        "Approval flow is incomplete.",
                        "All approval levels must be approved before creating the item master.");
                }

                if (isExcel)
                {
                    // ---------------------------------------------------
                    // Final approval for the batch: re-read the stored
                    // Excel asset and create every valid row's
                    // ItemBuyerMaster now. This is the ONLY point where
                    // Excel rows are ever written to ItemBuyerMaster.
                    // Everything below (row creation + status update) is
                    // staged via the repository and committed together in
                    // one SaveAsync, so a failure here never leaves the
                    // batch marked COMPLETE without the rows existing.
                    // ---------------------------------------------------

                    await CreateItemBuyerMastersFromExcelAsync(
                        excelMaterialMaster,
                        cancellationToken);

                    excelMaterialMaster.Status = Common.COMPLETE;

                    _repositoryWrapper.ExcelMaterialMaster
                        .Update(excelMaterialMaster);
                }
                else
                {
                    // -----------------------------------------------------
                    // 13. Check Item Buyer Master Already Exists
                    // -----------------------------------------------------

                    var existingItemMaster =
                        await _repositoryWrapper.ItemBuyerMaster
                            .FindFirstByConditionAsync(x =>
                                x.MaterialCode ==
                                predefinedMaterial.MaterialCode &&
                                x.IsActive);

                    // -----------------------------------------------------
                    // 14. Create Item Buyer Master
                    // -----------------------------------------------------

                    if (existingItemMaster == null)
                    {
                        var itemMaster = new ItemBuyerMaster
                        {
                            Id = Guid.NewGuid(),

                            BuyerId =
                                predefinedMaterial.BuyerId,

                            Description =
                                predefinedMaterial.Description,

                            MaterialCode =
                                predefinedMaterial.MaterialCode,

                            MaterialGroup =
                                predefinedMaterial.MaterialGroup,

                            ProductType =
                                predefinedMaterial.ProductType,

                            BaseUnitOfMeasure =
                                predefinedMaterial.BaseUnitOfMeasure,

                            OrderUnitOfMeasure =
                                predefinedMaterial.OrderUnitOfMeasure,

                            AlternateUnitOfMeasure =
                                predefinedMaterial.AlternateUnitOfMeasure,

                            ValuationClass =
                                predefinedMaterial.ValuationClass,

                            UnitOfMeasureMapping =
                                predefinedMaterial.UnitOfMeasureMapping,

                            SubUnit =
                                predefinedMaterial.SubUnit,

                            MicroUnit =
                                predefinedMaterial.MicroUnit
                        };

                        await _repositoryWrapper.ItemBuyerMaster
                            .CreateAsync(itemMaster);

                        _logger.LogInfo(
                            $"Item Buyer Master created successfully. " +
                            $"MaterialCode: {predefinedMaterial.MaterialCode}");
                    }

                    // -----------------------------------------------------
                    // 15. Final Approval Completed
                    // -----------------------------------------------------

                    predefinedMaterial.Status =
                        Common.COMPLETE;

                    _repositoryWrapper
                        .PredefinedMaterial
                        .Update(predefinedMaterial);
                }
            }
            else
            {
                // -----------------------------------------------------
                // 16. Approval Is Still In Progress
                // -----------------------------------------------------

                if (isExcel)
                {
                    excelMaterialMaster.Status = Common.PROCESSING;

                    _repositoryWrapper.ExcelMaterialMaster
                        .Update(excelMaterialMaster);
                }
                else
                {
                    predefinedMaterial.Status =
                        Common.PROCESSING;

                    _repositoryWrapper
                        .PredefinedMaterial
                        .Update(predefinedMaterial);
                }
            }

            // ---------------------------------------------------------
            // 17. Save Changes
            // ---------------------------------------------------------

            await _repositoryWrapper.SaveAsync();

            var resultId = isExcel ? excelMaterialMaster.Id : predefinedMaterial.Id;
            var resultStatus = isExcel ? excelMaterialMaster.Status : predefinedMaterial.Status;

            _logger.LogInfo(
                $"Material approval processed successfully. " +
                $"PredefinedMaterialId: {resultId}, " +
                $"UserId: {request.UserId}, " +
                $"Status: {request.Approval.Status}, " +
                $"MaterialStatus: {resultStatus}");

            return resultId;
        }

        /// <summary>
        /// Re-reads the Excel batch's stored Asset and creates an
        /// ItemBuyerMaster row for every valid, non-duplicate row - the
        /// same 11 columns the upload endpoint validates. Existing
        /// MaterialCode values are skipped (not duplicated), same as the
        /// manual flow.
        /// </summary>
        private async Task CreateItemBuyerMastersFromExcelAsync(
            ExcelMaterialMaster excelMaterialMaster,
            CancellationToken cancellationToken)
        {
            var document = await _mediator.Send(
                new GetDocumentQuery(excelMaterialMaster.AssetId),
                cancellationToken);

            using var workbook = new XLWorkbook(new MemoryStream(document.FileBytes));
            var worksheet = workbook.Worksheet(1);

            var existingMaterialCodes = (await _repositoryWrapper.ItemBuyerMaster
                    .FindByCondition(x => x.IsActive)
                    .Select(x => x.MaterialCode)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newItems = new List<ItemBuyerMaster>();
            int skippedDuplicates = 0;
            int skippedBlankMaterialCode = 0;

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var materialCode = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(materialCode))
                {
                    skippedBlankMaterialCode++;
                    continue;
                }

                if (existingMaterialCodes.Contains(materialCode))
                {
                    skippedDuplicates++;
                    continue;
                }

                newItems.Add(new ItemBuyerMaster
                {
                    Id = Guid.NewGuid(),
                    BuyerId = excelMaterialMaster.BuyerId,
                    Description = row.Cell(1).GetString().Trim(),
                    MaterialCode = materialCode,
                    MaterialGroup = row.Cell(3).GetString().Trim(),
                    ProductType = row.Cell(4).GetString().Trim(),
                    BaseUnitOfMeasure = row.Cell(5).GetString().Trim(),
                    OrderUnitOfMeasure = row.Cell(6).GetString().Trim(),
                    AlternateUnitOfMeasure = row.Cell(7).GetString().Trim(),
                    ValuationClass = row.Cell(8).GetString().Trim(),
                    UnitOfMeasureMapping = row.Cell(9).GetString().Trim(),
                    SubUnit = string.IsNullOrWhiteSpace(row.Cell(10).GetString())
                        ? null : row.Cell(10).GetString().Trim(),
                    MicroUnit = string.IsNullOrWhiteSpace(row.Cell(11).GetString())
                        ? null : row.Cell(11).GetString().Trim()
                });

                // Guard against duplicate MaterialCode values within the
                // same file.
                existingMaterialCodes.Add(materialCode);
            }

            if (newItems.Any())
            {
                await _repositoryWrapper.ItemBuyerMaster
                    .CreateRangeAsync(newItems);
            }

            // Always log the outcome - including when nothing new gets
            // created - so a batch reaching COMPLETE with zero new rows
            // (e.g. every MaterialCode already existed) is visible instead
            // of silent.
            _logger.LogInfo(
                $"Excel batch final approval processed. " +
                $"ExcelMaterialMasterId: {excelMaterialMaster.Id}, " +
                $"Created: {newItems.Count}, " +
                $"SkippedDuplicateMaterialCode: {skippedDuplicates}, " +
                $"SkippedBlankMaterialCode: {skippedBlankMaterialCode}");
        }
    }
}
