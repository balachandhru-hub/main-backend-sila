using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;
using static SilaMe.Api.Services.PermissionKeys;

namespace SilaMe.Api.Services;

public static class AccessSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SilaMeDbContext>();
        var now = DateTime.UtcNow;
        await SeedCatalogAsync(db, now);

        var environment = services.GetRequiredService<IHostEnvironment>();
        if (environment.IsDevelopment())
        {

        var organization = await db.Organizations.SingleOrDefaultAsync(candidate => candidate.Code == "SILA-DEMO");
        if (organization is null)
        {
            organization = new Organization
            {
                Id = Guid.NewGuid(),
                Code = "SILA-DEMO",
                Name = "SILA ME Demo Organization",
                Kind = OrganizationKind.CUSTOMER,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Organizations.Add(organization);
        }

        var admin = await db.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == "BALA@CHERVIC.IN");
        if (admin is not null && !await db.UserOrganizationMemberships.AnyAsync(membership =>
                membership.UserId == admin.Id && membership.OrganizationId == organization.Id && membership.Status == StatusKind.ACTIVE))
        {
            // Do not mutate the protected administrator's memberships or roles from
            // development seeding. Existing access is preserved exactly as found.
        }

        await SeedPhase3DemoAsync(db, organization, now);
        await db.SaveChangesAsync();
        }

        var operational = services.GetService<OperationalDatabaseOptions>();
        if (!environment.IsEnvironment("Testing"))
        {
            await MasterDataRestore.RestoreAsync(db, operational);
        }
    }

    public static async Task SeedCatalogAsync(SilaMeDbContext db, DateTime now)
    {
        var permissions = new Dictionary<string, Permission>();
        foreach (var definition in PermissionKeys.All)
        {
            var permission = await db.Permissions.SingleOrDefaultAsync(candidate => candidate.Key == definition.Key);
            if (permission is null)
            {
                permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = definition.Key,
                    Name = definition.Name,
                    Description = definition.Description,
                    Module = definition.Module,
                    SubModule = definition.SubModule,
                    ApplicationScope = definition.ApplicationScope,
                    RiskLevel = definition.RiskLevel,
                    IsSystemAuthorization = definition.IsSystemAuthorization,
                    IsActive = true,
                    CreatedAt = now,
                };
                db.Permissions.Add(permission);
            }
            else
            {
                permission.Name = definition.Name;
                permission.Description = definition.Description;
                permission.Module = definition.Module;
                permission.SubModule = definition.SubModule;
                permission.ApplicationScope = definition.ApplicationScope;
                permission.RiskLevel = definition.RiskLevel;
                permission.IsSystemAuthorization = definition.IsSystemAuthorization;
                permission.IsActive = true;
            }
            permissions[definition.Key] = permission;
        }

        var roles = new Dictionary<string, Role>();
        foreach (var definition in new[]
        {
            ("SUPER_ADMIN", "Super administrator", "Highest SILA ME internal administrative role.", "BOTH", true, PermissionKeys.All.Select(permission => permission.Key).ToArray()),
            ("PLATFORM_ADMIN", "Platform administrator", "Technical platform and integration administration.", "CLOUD", true, new[] { "VIEW_PLATFORM", "MANAGE_PLATFORM", "VIEW_INTEGRATION", "MANAGE_INTEGRATION", "TEST_INTEGRATION", ManageIntegrationMapping, RunIntegration, ViewIntegrationLogs, ViewIntegrationData, "VIEW_SYSTEM_DIAGNOSTICS", "VIEW_SYSTEM_HEALTH", ViewAuditLog, "VIEW_SECURITY_AUDIT", ManageDocumentStorage, ValidateDocumentStorage, DisconnectDocumentStorage }),
            ("CUSTOMER_ADMIN", "Customer administrator", "Customer-scoped administration.", "CLOUD", true, new[] { OrganizationRead, OrganizationManage, UserRead, UserManage, RoleRead, RoleManage, AccessManage, AssignRole, AssignScope, ViewPermission, ViewDocumentStorage, ManageDocumentStorage, ValidateDocumentStorage, ViewAuditLog, "VIEW_INTEGRATION", "MANAGE_INTEGRATION", "TEST_INTEGRATION", ManageIntegrationMapping, RunIntegration, ViewIntegrationLogs, ViewIntegrationData }),
            ("CUSTOMER_SUPPORT_CONSULTANT", "Customer support consultant", "Diagnostic and read-oriented customer support access.", "CLOUD", true, new[] { OrganizationRead, UserRead, RoleRead, ViewInvoice, ViewPurchaseOrder, ViewGrn, ViewExtraction, ViewAuditLog }),
            ("OPERATIONS_ADMIN", "Operations administrator", "Configure and supervise operational functions.", "BOTH", true, new[] { OrganizationRead, ViewInvoice, EditInvoice, ViewPurchaseOrder, MatchPurchaseOrder, ViewGrn, CreateGrn, EditGrn, ValidateGrn, ViewInventoryKey(), ViewDashboardKey() }),
            ("OPERATIONS_MANAGER", "Operations manager", "Overall operational control within assigned scope.", "BOTH", true, new[] { OrganizationRead, ViewInvoice, EditInvoice, ViewPurchaseOrder, MatchPurchaseOrder, ViewGrn, CreateGrn, EditGrn, ValidateGrn, ViewExtraction, ViewDashboardKey(), "VIEW_OPERATION_ALERTS", "VIEW_KPI", "VIEW_INVENTORY", "VIEW_STOCK_BALANCE" }),
            ("MENU_MANAGER", "Menu manager", "Menu engineering and recipe access.", "CLOUD", true, new[] { "VIEW_MENU", "CREATE_MENU", "EDIT_MENU", "FINALIZE_MENU", "VIEW_RECIPE", "CREATE_RECIPE", "EDIT_RECIPE", "APPROVE_RECIPE", "MANAGE_RECIPE_WORKFLOW", "VIEW_POS_INTEGRATION", "MANAGE_POS_INTEGRATION", "VIEW_RECIPE_TRANSACTION", "REPROCESS_RECIPE_TRANSACTION", "VIEW_MATERIAL", "CREATE_MATERIAL", "EDIT_MATERIAL", "APPROVE_MATERIAL" }),
            ("INVENTORY_MANAGER", "Inventory manager", "Inventory control within assigned scope.", "BOTH", true, new[] { OrganizationRead, "VIEW_INVENTORY", "VIEW_STOCK_BALANCE", "VIEW_STOCK_TRANSACTION", "CREATE_INVENTORY_COUNT", "ENTER_INVENTORY_COUNT", "APPROVE_INVENTORY_COUNT", "POST_INVENTORY_COUNT", "CREATE_STOCK_ADJUSTMENT", "APPROVE_STOCK_ADJUSTMENT", "POST_STOCK_ADJUSTMENT", "POST_OPENING_STOCK", "CREATE_STOCK_TRANSFER", "CREATE_INTERNAL_TRANSFER", "APPROVE_STOCK_TRANSFER", "DISPATCH_STOCK_TRANSFER", "DISPATCH_INTERNAL_TRANSFER", "RECEIVE_STOCK_TRANSFER", "RECEIVE_INTERNAL_TRANSFER", "VIEW_GOODS_ISSUE", "CREATE_GOODS_ISSUE", "VIEW_WASTE", "CREATE_WASTE", "VIEW_BATCH", "VIEW_EXPIRY", "VIEW_LOCATION", "MANAGE_LOCATION" }),
            ("PROCUREMENT_MANAGER", "Procurement manager", "Purchasing and supplier management.", "CLOUD", true, new[] { OrganizationRead, ViewPurchaseOrder, "VIEW_MATERIAL_REQUIREMENT", "VIEW_PURCHASE_SUGGESTION", "REVIEW_PURCHASE_SUGGESTION", "APPROVE_PURCHASE_SUGGESTION", "VIEW_SUPPLIER", "CREATE_SUPPLIER", "EDIT_SUPPLIER" }),
            ("RECEIVING_MANAGER", "Receiving manager", "Invoice receiving, matching, and GRN supervision.", "BOTH", true, new[] { OrganizationRead, ScanInvoice, UploadInvoice, ViewInvoice, EditInvoice, ViewPurchaseOrder, MatchPurchaseOrder, ViewGrn, CreateGrn, EditGrn, ValidateGrn, "ENTER_RECEIVED_QUANTITY", "ENTER_ACCEPTED_QUANTITY", "ENTER_DAMAGED_QUANTITY", "ENTER_REJECTED_QUANTITY", "VIEW_RECEIVING_EXCEPTION", "RESOLVE_RECEIVING_EXCEPTION", ViewExtraction, "RUN_BASIC_OCR", "REREAD_INVOICE" }),
            ("STORE_OPERATOR", "Store operator", "General mobile operational execution.", "MOBILE", true, new[] { OrganizationRead, "VIEW_INVENTORY", "VIEW_STOCK_BALANCE", "CREATE_INVENTORY_COUNT", "ENTER_INVENTORY_COUNT", "CREATE_STOCK_TRANSFER", "CREATE_INTERNAL_TRANSFER", "DISPATCH_STOCK_TRANSFER", "DISPATCH_INTERNAL_TRANSFER", "RECEIVE_STOCK_TRANSFER", "RECEIVE_INTERNAL_TRANSFER", "VIEW_GOODS_ISSUE", "CREATE_GOODS_ISSUE", "VIEW_WASTE", "CREATE_WASTE", ScanInvoice }),
            ("RECEIVING_OPERATOR", "Receiving operator", "Mobile invoice receiving and review.", "MOBILE", true, new[] { OrganizationRead, ScanInvoice, UploadInvoice, ViewInvoice, ViewPurchaseOrder, MatchPurchaseOrder, ViewGrn, CreateGrn, ValidateGrn, "ENTER_RECEIVED_QUANTITY", "ENTER_ACCEPTED_QUANTITY", "ENTER_DAMAGED_QUANTITY", "ENTER_REJECTED_QUANTITY", "RUN_BASIC_OCR", "REREAD_INVOICE" }),
            ("INVENTORY_OPERATOR", "Inventory operator", "Mobile inventory execution.", "MOBILE", true, new[] { OrganizationRead, "VIEW_INVENTORY", "VIEW_STOCK_BALANCE", "CREATE_INVENTORY_COUNT", "ENTER_INVENTORY_COUNT", "SUBMIT_INVENTORY_COUNT", "CREATE_STOCK_TRANSFER", "CREATE_INTERNAL_TRANSFER", "DISPATCH_STOCK_TRANSFER", "DISPATCH_INTERNAL_TRANSFER", "RECEIVE_STOCK_TRANSFER", "RECEIVE_INTERNAL_TRANSFER", "VIEW_GOODS_ISSUE", "CREATE_GOODS_ISSUE", "VIEW_WASTE", "CREATE_WASTE" }),
            ("APPROVER", "Approver", "Approval workflow access.", "BOTH", true, new[] { "VIEW_APPROVAL", "APPROVE", "REJECT", "RETURN_FOR_CORRECTION", "VIEW_APPROVAL_HISTORY", "VIEW_WORKFLOW", "VIEW_RECIPE", "APPROVE_RECIPE" }),
            ("FINANCE_REVIEWER", "Finance reviewer", "Invoice, document, and financial exception review.", "CLOUD", true, new[] { ViewInvoice, UploadInvoice, EditInvoice, ViewExtraction, ViewPurchaseOrder, MatchPurchaseOrder, ViewGrn, "VIEW_INVOICE_EXCEPTION", "RESOLVE_INVOICE_EXCEPTION", "VIEW_ANALYTICS" }),
            ("ANALYST", "Analyst", "Read-oriented operational analytics.", "CLOUD", true, new[] { "VIEW_DASHBOARD", "VIEW_KPI", "VIEW_ANALYTICS", "VIEW_MENU_ANALYTICS", "VIEW_CONSUMPTION_ANALYTICS", "VIEW_FOOD_COST_ANALYTICS", "VIEW_INVENTORY_ANALYTICS", "VIEW_WASTE_ANALYTICS", "VIEW_PURCHASE_ANALYTICS", "VIEW_REPORT", "EXPORT_REPORT" }),
            ("AUDITOR", "Auditor", "Read-only audit and operational history.", "CLOUD", true, new[] { "VIEW_DASHBOARD", ViewInvoice, ViewPurchaseOrder, ViewGrn, "VIEW_STOCK_TRANSACTION", ViewExtraction, "VIEW_EXTRACTION_HISTORY", ViewAuditLog, "VIEW_SECURITY_AUDIT", "VIEW_APPROVAL_HISTORY" }),
            ("VIEWER", "Viewer", "Generic read-only access within assigned scope.", "BOTH", true, new[] { OrganizationRead, "VIEW_DASHBOARD", ViewInvoice, ViewPurchaseOrder, ViewGrn, "VIEW_INVENTORY", "VIEW_STOCK_BALANCE", ViewExtraction }),
        })
        {
            var role = await db.Roles
                .Include(candidate => candidate.Permissions)
                .SingleOrDefaultAsync(candidate => candidate.Key == definition.Item1);
            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Key = definition.Item1,
                    Name = definition.Item2,
                    Description = definition.Item3,
                    IsSystem = definition.Item5,
                    ApplicationScope = definition.Item4,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.Roles.Add(role);
            }

            role.Name = definition.Item2;
            role.Description = definition.Item3;
            role.ApplicationScope = definition.Item4;
            role.IsSystem = definition.Item5;
            roles[definition.Item1] = role;
            foreach (var permissionKey in definition.Item6)
            {
                if (permissions.TryGetValue(permissionKey, out var permission) &&
                    !role.Permissions.Any(mapping => mapping.PermissionId == permission.Id))
                {
                    role.Permissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                    });
                }
            }
        }
    }

    private static string ViewInventoryKey() => "VIEW_INVENTORY";
    private static string ViewDashboardKey() => "VIEW_DASHBOARD";

    private static async Task SeedPhase3DemoAsync(SilaMeDbContext db, Organization organization, DateTime now)
    {
        var unit = await db.OrganizationUnits.SingleOrDefaultAsync(item =>
            item.OrganizationId == organization.Id && item.Code == "DEMO-MAIN-STORE");
        if (unit is null)
        {
            unit = new OrganizationUnit
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Code = "DEMO-MAIN-STORE",
                Name = "Demo Hotel Main Store",
                Kind = OrganizationUnitKind.STORE,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.OrganizationUnits.Add(unit);
        }

        var supplier = await db.Suppliers.SingleOrDefaultAsync(item =>
            item.OrganizationId == organization.Id && item.SupplierCode == "DEMO-SUPPLIER");
        if (supplier is null)
        {
            supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                SupplierCode = "DEMO-SUPPLIER",
                Name = "Demo Hospitality Supplier",
                NormalizedName = "DEMO HOSPITALITY SUPPLIER",
                TaxNumber = "DEMO-TAX-001",
                Status = StatusKind.ACTIVE,
                SourceSystem = "DEVELOPMENT_SEED",
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Suppliers.Add(supplier);
        }

        var materials = new[]
        {
            ("CHICKEN-BREAST", "Chicken Breast", "KG"),
            ("BASMATI-RICE", "Basmati Rice", "KG"),
            ("COOKING-OIL", "Cooking Oil", "L"),
        };
        var materialEntities = new Dictionary<string, Material>();
        foreach (var definition in materials)
        {
            var material = await db.Materials.SingleOrDefaultAsync(item =>
                item.OrganizationId == organization.Id && item.MaterialCode == definition.Item1);
            if (material is null)
            {
                material = new Material
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    MaterialCode = definition.Item1,
                    Description = definition.Item2,
                    NormalizedDescription = definition.Item2.ToUpperInvariant(),
                    BaseUom = definition.Item3,
                    Status = StatusKind.ACTIVE,
                    SourceSystem = "DEVELOPMENT_SEED",
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.Materials.Add(material);
            }
            materialEntities[definition.Item1] = material;
        }

        if (!await db.SupplierMaterials.AnyAsync(item => item.SupplierId == supplier.Id && item.MaterialId == materialEntities["CHICKEN-BREAST"].Id))
        {
            foreach (var material in materialEntities.Values)
            {
                db.SupplierMaterials.Add(new SupplierMaterial
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    SupplierId = supplier.Id,
                    MaterialId = material.Id,
                    SupplierMaterialCode = $"SUP-{material.MaterialCode}",
                    SupplierDescription = material.Description,
                    PurchaseUom = material.BaseUom,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        var purchaseOrder = await db.PurchaseOrders
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.OrganizationId == organization.Id && item.PoNumber == "4500001001");
        if (purchaseOrder is null)
        {
            purchaseOrder = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                OperatingUnitId = unit.Id,
                SupplierId = supplier.Id,
                PoNumber = "4500001001",
                PoDate = DateOnly.FromDateTime(now.AddDays(-5)),
                DeliveryDate = DateOnly.FromDateTime(now),
                Currency = "AED",
                Status = PurchaseOrderStatus.OPEN,
                SourceSystem = "DEVELOPMENT_SEED",
                CreatedAt = now,
                UpdatedAt = now,
            };
            purchaseOrder.Items =
            [
                DemoPoItem(purchaseOrder.Id, 10, materialEntities["CHICKEN-BREAST"], 100, "KG", now),
                DemoPoItem(purchaseOrder.Id, 20, materialEntities["BASMATI-RICE"], 200, "KG", now),
                DemoPoItem(purchaseOrder.Id, 30, materialEntities["COOKING-OIL"], 50, "L", now),
            ];
            db.PurchaseOrders.Add(purchaseOrder);
        }

        var invoice = await db.Invoices
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.OrganizationId == organization.Id && item.InvoiceNumber == "INV-1001");
        if (invoice is null)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                OperatingUnitId = unit.Id,
                UploadedByUserId = (await db.Users.Select(user => user.Id).FirstAsync()),
                DocumentType = DocumentType.INVOICE,
                OriginalFilename = "demo-invoice.txt",
                ContentType = "text/plain",
                FileSizeBytes = 0,
                StorageProvider = "DEVELOPMENT_SEED",
                StorageReference = "seed/demo-invoice.txt",
                Status = DocumentStatus.PROCESSED,
                SourceChannel = DocumentSourceChannel.MOBILE_UPLOAD,
                CreatedAt = now,
                UpdatedAt = now,
            };
            invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                OperatingUnitId = unit.Id,
                DocumentId = document.Id,
                SupplierId = supplier.Id,
                InvoiceNumber = "INV-1001",
                InvoiceDate = DateOnly.FromDateTime(now),
                SupplierNameRaw = supplier.Name,
                SupplierTaxNumberRaw = supplier.TaxNumber,
                PoNumberRaw = purchaseOrder.PoNumber,
                PurchaseOrderId = purchaseOrder.Id,
                Currency = "AED",
                NetAmount = 5150m,
                TaxAmount = 257.5m,
                GrossAmount = 5407.5m,
                InvoiceType = InvoiceType.MATERIAL,
                Status = InvoiceStatus.READY_FOR_GRN,
                OverallConfidence = 0.95m,
                CreatedByUserId = document.UploadedByUserId,
                CreatedAt = now,
                UpdatedAt = now,
                Lines =
                [
                    DemoInvoiceLine(10, materialEntities["CHICKEN-BREAST"], 50, "KG", purchaseOrder.Items.Single(item => item.LineNumber == 10).Id, now),
                    DemoInvoiceLine(20, materialEntities["BASMATI-RICE"], 80, "KG", purchaseOrder.Items.Single(item => item.LineNumber == 20).Id, now),
                    DemoInvoiceLine(30, materialEntities["COOKING-OIL"], 20, "L", purchaseOrder.Items.Single(item => item.LineNumber == 30).Id, now),
                ],
            };
            db.Documents.Add(document);
            db.Invoices.Add(invoice);
        }
    }

    private static PurchaseOrderItem DemoPoItem(Guid purchaseOrderId, int lineNumber, Material material, decimal quantity, string uom, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        PurchaseOrderId = purchaseOrderId,
        LineNumber = lineNumber,
        MaterialId = material.Id,
        MaterialCode = material.MaterialCode,
        Description = material.Description,
        OrderedQuantity = quantity,
        ReceivedQuantity = 0,
        OpenQuantity = quantity,
        Uom = uom,
        UnitPrice = lineNumber == 10 ? 22.5m : lineNumber == 20 ? 12.5m : 8m,
        Status = PurchaseOrderItemStatus.OPEN,
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static InvoiceLine DemoInvoiceLine(int lineNumber, Material material, decimal quantity, string uom, Guid purchaseOrderItemId, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        LineNumber = lineNumber,
        SupplierMaterialCode = $"SUP-{material.MaterialCode}",
        MaterialId = material.Id,
        MaterialCodeRaw = material.MaterialCode,
        DescriptionRaw = material.Description,
        Quantity = quantity,
        Uom = uom,
        UnitPrice = lineNumber == 10 ? 22.5m : lineNumber == 20 ? 12.5m : 8m,
        LineAmount = quantity * (lineNumber == 10 ? 22.5m : lineNumber == 20 ? 12.5m : 8m),
        Confidence = 0.95m,
        MatchStatus = InvoiceLineMatchStatus.MATCHED,
        PurchaseOrderItemId = purchaseOrderItemId,
        CreatedAt = now,
        UpdatedAt = now,
    };
}