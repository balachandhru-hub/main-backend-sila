using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class MasterDataRestoreTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Restores_id_label_supplier_names_and_copies_pos_into_empty_org()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var now = DateTime.UtcNow;
        var sourceOrg = new Organization
        {
            Id = Guid.NewGuid(), Code = $"SRC-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Name = "Source property", Kind = OrganizationKind.CUSTOMER, Status = StatusKind.ACTIVE,
            CreatedAt = now, UpdatedAt = now,
        };
        var emptyOrg = new Organization
        {
            Id = Guid.NewGuid(), Code = $"DST-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Name = "Empty property", Kind = OrganizationKind.CUSTOMER, Status = StatusKind.ACTIVE,
            CreatedAt = now, UpdatedAt = now,
        };
        db.Organizations.AddRange(sourceOrg, emptyOrg);
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = sourceOrg.Id, EntityCode = "1050",
            SupplierCode = "1003430", Name = "ID: 1003430", NormalizedName = "ID 1003430",
            LegalName = "Test SBN", Status = StatusKind.ACTIVE, IsActive = true,
            SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        db.Suppliers.Add(supplier);
        db.PurchaseOrders.Add(new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = sourceOrg.Id, SupplierId = supplier.Id,
            PoNumber = "4500003415", EntityCode = "1050", CompanyCode = "1050", Currency = "AED",
            SupplierName = "Test SBN", Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST",
            CreatedAt = now, UpdatedAt = now,
            Items =
            [
                new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(), LineNumber = 10, MaterialCode = "MAT-1", Description = "Material",
                    OrderedQuantity = 10, ReceivedQuantity = 0, OpenQuantity = 10, Uom = "KG",
                    GoodsReceiptExpected = true, Status = PurchaseOrderItemStatus.OPEN,
                    CreatedAt = now, UpdatedAt = now,
                },
            ],
        });
        await db.SaveChangesAsync();

        await MasterDataRestore.RestoreAsync(db);

        var restored = await db.Suppliers.SingleAsync(item => item.Id == supplier.Id);
        Assert.Equal("Test SBN", restored.Name);
        Assert.True(await db.Suppliers.CountAsync(item => item.OrganizationId == emptyOrg.Id && !item.IsDeleted) > 0);
        Assert.True(await db.PurchaseOrders.CountAsync(item => item.OrganizationId == emptyOrg.Id) > 0);
    }
}
