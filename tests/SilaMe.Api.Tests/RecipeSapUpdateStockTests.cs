using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class RecipeSapUpdateStockTests
{
    [Fact]
    public void Movement_type_is_z02_for_negative_adjustment_and_quantity_is_absolute()
    {
        Assert.Equal("Z02", RecipeSapUpdateStock.MovementType(-5m));
        Assert.Equal("Z01", RecipeSapUpdateStock.MovementType(5m));
        Assert.Equal("5.000", RecipeSapUpdateStock.AbsoluteQuantity(-5m));
        Assert.Equal("5.000", RecipeSapUpdateStock.AbsoluteQuantity(5m));
    }

    [Fact]
    public void Request_body_matches_tested_update_stock_shape()
    {
        var json = RecipeSapUpdateStock.BuildRequestJson(new DateOnly(2026, 9, 28),
        [
            new RecipeSapStockItem(new DateOnly(2026, 9, 28), "1060", "FL45", "MAT1001", "Z02", 5m, "PC", [Guid.NewGuid()]),
        ]);
        Assert.Contains("\"PostingDate\":\"2026-09-28T00:00:00\"", json);
        Assert.Contains("\"DocumentDate\":\"2026-09-28T00:00:00\"", json);
        Assert.Contains("\"GoodsMovementCode\":\"03\"", json);
        Assert.Contains("\"to_MaterialDocumentItem\"", json);
        Assert.Contains("\"GoodsMovementType\":\"Z02\"", json);
        Assert.Contains("\"QuantityInEntryUnit\":\"5.000\"", json);
        Assert.DoesNotContain("-5.000", json);
        Assert.DoesNotContain("\"processType\"", json);
    }

    [Fact]
    public void Tracker_steps_separate_local_inventory_from_erp_result()
    {
        var txn = new RecipeConsumptionTransaction
        {
            Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), PosSourceId = Guid.NewGuid(),
            SourceSystem = "EXCEL", SourceTransactionId = "TXN1", PosOutletCode = "POOL-BAR", PosItemCode = "GIN001",
            Status = RecipeTransactionStatus.POSTED, ExternalReference = "4900123456",
        };
        var posting = new RecipeInventoryPosting
        {
            Id = Guid.NewGuid(), TransactionId = txn.Id, OrganizationId = txn.OrganizationId,
            Status = RecipeTransactionStatus.POSTED, HttpStatus = 201, MaterialDocument = "4900123456",
            ResponseJson = "{\"MaterialDocument\":\"4900123456\"}", CreatedAt = DateTime.UtcNow,
        };
        var steps = RecipeSapUpdateStock.TrackerSteps(txn, posting, null);
        Assert.Equal("UPDATED", steps.Step1Status);
        Assert.Equal("SUCCESS", steps.Step2Status);
        Assert.Contains("4900123456", steps.Step2Message);
        Assert.Contains("ERP response", steps.Step2Message);

        txn.Status = RecipeTransactionStatus.FAILED;
        txn.FailedStep = "S4_POSTING";
        txn.FailureMessage = "SAP posting failed";
        posting.Status = RecipeTransactionStatus.FAILED;
        posting.ErrorMessage = "SAP posting failed";
        var failed = RecipeSapUpdateStock.TrackerSteps(txn, posting, null);
        Assert.Equal("UPDATED", failed.Step1Status);
        Assert.Equal("FAILED", failed.Step2Status);

        txn.Status = RecipeTransactionStatus.RECEIVED;
        txn.FailedStep = null;
        txn.FailureMessage = null;
        var missedStep1 = RecipeSapUpdateStock.TrackerSteps(txn, null, null);
        Assert.Equal("FAILED", missedStep1.Step1Status);
        Assert.Equal("NOT_STARTED", missedStep1.Step2Status);
        Assert.Contains("not updated", missedStep1.Step1Message, StringComparison.OrdinalIgnoreCase);

        txn.Status = RecipeTransactionStatus.READY_TO_POST;
        posting.ErrorMessage = null;
        var missedStep2 = RecipeSapUpdateStock.TrackerSteps(txn, posting, null);
        Assert.Equal("UPDATED", missedStep2.Step1Status);
        Assert.Equal("FAILED", missedStep2.Step2Status);
        Assert.Contains("did not complete", missedStep2.Step2Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reprocess_is_available_for_failed_or_not_updated_rows()
    {
        Assert.True(RecipeSapUpdateStock.CanReprocess(RecipeTransactionStatus.FAILED));
        Assert.True(RecipeSapUpdateStock.CanReprocess(RecipeTransactionStatus.READY_TO_POST));
        Assert.True(RecipeSapUpdateStock.CanReprocess(RecipeTransactionStatus.POSTING_UNKNOWN));
        Assert.True(RecipeSapUpdateStock.CanReprocess(RecipeTransactionStatus.RECEIVED));
        Assert.False(RecipeSapUpdateStock.CanReprocess(RecipeTransactionStatus.POSTED));
        Assert.False(RecipeSapUpdateStock.CanReprocess(RecipeTransactionStatus.POSTING));
    }

    [Fact]
    public void Aggregation_keeps_incompatible_posting_groups_apart()
    {
        var date = new DateOnly(2026, 9, 28);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var items = RecipeSapUpdateStock.Aggregate(
        [
            new RecipeSapStockItem(date, "1060", "FL45", "MAT1001", "Z02", 30m, "ML", [a]),
            new RecipeSapStockItem(date, "1060", "FL45", "MAT1001", "Z02", 60m, "ML", [b]),
            new RecipeSapStockItem(date, "1060", "FL45", "MAT1001", "Z01", 5m, "ML", [Guid.NewGuid()]),
        ]);
        Assert.Equal(2, items.Count);
        Assert.Equal(90m, items.Single(item => item.GoodsMovementType == "Z02").Quantity);
        Assert.Equal(2, items.Single(item => item.GoodsMovementType == "Z02").TransactionIds.Count);
    }
}
