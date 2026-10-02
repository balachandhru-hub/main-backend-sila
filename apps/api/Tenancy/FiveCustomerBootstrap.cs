using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Tenancy;

public sealed record OperationalTableClassification(string Table, string Class, string Disposition);

public static class FiveCustomerBootstrap
{
    public static readonly OperationalTableClassification[] Inventory =
    [
        new("organizations", "A. CUSTOMER MASTER", "Copy SILA-DEMO/TEST-PHASE2 into FIVE"),
        new("organization_units", "A. CUSTOMER MASTER", "Copy linked units"),
        new("suppliers", "A. CUSTOMER MASTER", "Copy including 1003430 Test SBN"),
        new("supplier_aliases", "A. CUSTOMER MASTER", "Copy FKs"),
        new("materials", "A. CUSTOMER MASTER", "Copy"),
        new("supplier_materials", "A. CUSTOMER MASTER", "Copy FKs"),
        new("purchase_orders", "B. CUSTOMER TRANSACTION", "Copy including 4500003415"),
        new("purchase_order_items", "B. CUSTOMER TRANSACTION", "Copy items unchanged"),
        new("invoices", "B. CUSTOMER TRANSACTION", "Copy useful demo invoices"),
        new("invoice_lines", "B. CUSTOMER TRANSACTION", "Copy FKs"),
        new("invoice_ext_data", "B. CUSTOMER TRANSACTION", "Copy FKs"),
        new("goods_receipts", "B. CUSTOMER TRANSACTION", "Copy if present"),
        new("goods_receipt_lines", "B. CUSTOMER TRANSACTION", "Copy FKs"),
        new("documents", "B. CUSTOMER TRANSACTION", "Copy linked"),
        new("document_pages", "B. CUSTOMER TRANSACTION", "Copy FKs"),
        new("document_extractions", "B. CUSTOMER TRANSACTION", "Copy FKs"),
        new("inventory_transactions", "B. CUSTOMER TRANSACTION", "Copy if linked"),
        new("stock_balances", "B. CUSTOMER TRANSACTION", "Copy if linked"),
        new("api_integration_configurations", "C. CUSTOMER CONFIGURATION", "Copy"),
        new("api_field_mappings", "C. CUSTOMER CONFIGURATION", "Copy FKs"),
        new("api_integration_executions", "C. CUSTOMER CONFIGURATION", "Skip executions"),
        new("extraction_agent_configs", "C. CUSTOMER CONFIGURATION", "Copy"),
        new("invoice_ocr_configurations", "C. CUSTOMER CONFIGURATION", "Copy"),
        new("document_storage_connections", "C. CUSTOMER CONFIGURATION", "Copy"),
        new("document_storage_destinations", "C. CUSTOMER CONFIGURATION", "Copy"),
        new("document_transfer_jobs", "C. CUSTOMER CONFIGURATION", "Skip jobs"),
        new("integration_schema_snapshots", "C. CUSTOMER CONFIGURATION", "Copy"),
        new("users", "D. CUSTOMER USER/RBAC", "Keep real identities only"),
        new("roles", "D. CUSTOMER USER/RBAC", "Copy catalog"),
        new("permissions", "D. CUSTOMER USER/RBAC", "Copy catalog"),
        new("role_permissions", "D. CUSTOMER USER/RBAC", "Copy catalog"),
        new("user_application_access", "D. CUSTOMER USER/RBAC", "Keep for retained users"),
        new("user_organization_memberships", "D. CUSTOMER USER/RBAC", "Keep FIVE memberships"),
        new("user_role_assignments", "D. CUSTOMER USER/RBAC", "Keep FIVE assignments"),
        new("user_authorization_overrides", "D. CUSTOMER USER/RBAC", "Keep if linked"),
        new("sessions", "D. CUSTOMER USER/RBAC", "Do not copy stale sessions"),
        new("audit_events", "D. CUSTOMER USER/RBAC", "Skip"),
        new("idempotency_records", "F. DEVELOPMENT-ONLY", "Skip"),
        new("microsoft_authorization_states", "F. DEVELOPMENT-ONLY", "Skip"),
    ];

    public static async Task EnsureAsync(
        IServiceProvider services,
        string operationalConnectionString,
        CancellationToken cancellationToken)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("FiveCustomerBootstrap");
        var testDb = TenantOperationalDatabase.Name(TenantOperationalDatabase.FiveTenantCode, TenantEnvironmentType.TEST);
        _ = environment;

        await BackupLegacyAsync(operationalConnectionString, logger, cancellationToken);
        await TenantOperationalDatabase.EnsureDatabaseExistsAsync(operationalConnectionString, testDb, cancellationToken);
        await CopyLegacyIntoTestIfNeededAsync(operationalConnectionString, testDb, logger, cancellationToken);
        await ConsolidateFiveTestAsync(TenantOperationalDatabase.Bind(operationalConnectionString, testDb), logger, cancellationToken);
        await ApplySupportedSchemaAsync(services, operationalConnectionString, cancellationToken);
        logger.LogInformation("FIVE customer databases are registered.");
    }

    public static async Task ApplySupportedSchemaAsync(
        IServiceProvider services,
        string operationalConnectionString,
        CancellationToken cancellationToken)
    {
        var platform = services.GetRequiredService<PlatformDbContext>();
        var passwords = services.GetRequiredService<IPasswordService>();
        var testDb = TenantOperationalDatabase.Name(TenantOperationalDatabase.FiveTenantCode, TenantEnvironmentType.TEST);
        var prodDb = TenantOperationalDatabase.Name(TenantOperationalDatabase.FiveTenantCode, TenantEnvironmentType.PRODUCTION);
        var testSecret = TenantOperationalDatabase.SecretReference(TenantOperationalDatabase.FiveTenantCode, TenantEnvironmentType.TEST);
        var prodSecret = TenantOperationalDatabase.SecretReference(TenantOperationalDatabase.FiveTenantCode, TenantEnvironmentType.PRODUCTION);

        await TenantOperationalDatabase.EnsureDatabaseExistsAsync(operationalConnectionString, testDb, cancellationToken);
        await TenantOperationalDatabase.EnsureDatabaseExistsAsync(operationalConnectionString, prodDb, cancellationToken);
        var testMigration = await MigrateAndSeedAsync(operationalConnectionString, testDb, passwords, cancellationToken);
        var prodMigration = await MigrateAndSeedAsync(operationalConnectionString, prodDb, passwords, cancellationToken);
        await EnsureFiveTestReceivingAccessAsync(TenantOperationalDatabase.Bind(operationalConnectionString, testDb), cancellationToken);
        await EnsureFiveTestReceivingMastersAsync(TenantOperationalDatabase.Bind(operationalConnectionString, testDb), cancellationToken);
        await EnsureProductionIsCleanAsync(TenantOperationalDatabase.Bind(operationalConnectionString, prodDb), passwords, cancellationToken);
        BindSecret(testSecret, TenantOperationalDatabase.Bind(operationalConnectionString, testDb));
        BindSecret(prodSecret, TenantOperationalDatabase.Bind(operationalConnectionString, prodDb));
        await RegisterFiveTenantAsync(platform, testSecret, prodSecret, cancellationToken);
        await RecordMigrationAsync(platform, TenantOperationalDatabase.FiveTenantCode, TenantEnvironmentType.TEST, testMigration, cancellationToken);
        await RecordMigrationAsync(platform, TenantOperationalDatabase.FiveTenantCode, TenantEnvironmentType.PRODUCTION, prodMigration, cancellationToken);
    }

    private static void BindSecret(string secretReference, string connectionString) =>
        Environment.SetEnvironmentVariable(TenantOperationalDatabase.SecretEnvironmentVariable(secretReference), connectionString);

    private static async Task BackupLegacyAsync(string operationalConnectionString, ILogger logger, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(DatabaseUrl.Normalize(operationalConnectionString));
        if (!string.Equals(builder.Database, TenantOperationalDatabase.LegacyOperationalDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), "sila-me-backups");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "sila_me_pre_five_migration.dump");
        if (File.Exists(path) && new FileInfo(path).Length > 0)
        {
            logger.LogInformation("Legacy backup already present at {Path}", path);
            return;
        }

        var psi = new ProcessStartInfo("pg_dump")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        psi.ArgumentList.Add("-Fc");
        psi.ArgumentList.Add("-h");
        psi.ArgumentList.Add(builder.Host ?? "localhost");
        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add(builder.Port.ToString());
        psi.ArgumentList.Add("-U");
        psi.ArgumentList.Add(builder.Username ?? "sila");
        psi.ArgumentList.Add("-d");
        psi.ArgumentList.Add(builder.Database);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add(path);
        psi.Environment["PGPASSWORD"] = builder.Password;
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("pg_dump could not start.");
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pg_dump failed: {error}");
        }

        logger.LogInformation("Wrote legacy backup {Path}", path);
    }

    private static async Task CopyLegacyIntoTestIfNeededAsync(
        string operationalConnectionString,
        string testDatabase,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var source = new NpgsqlConnectionStringBuilder(DatabaseUrl.Normalize(operationalConnectionString));
        if (!string.Equals(source.Database, TenantOperationalDatabase.LegacyOperationalDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var targetCs = TenantOperationalDatabase.Bind(operationalConnectionString, testDatabase);
        await using (var probe = new NpgsqlConnection(targetCs))
        {
            await probe.OpenAsync(cancellationToken);
            await using var count = new NpgsqlCommand("""SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'suppliers'""", probe);
            var hasSuppliersTable = Convert.ToInt64(await count.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
            if (hasSuppliersTable)
            {
                await using var rows = new NpgsqlCommand("""SELECT COUNT(*) FROM suppliers""", probe);
                if (Convert.ToInt64(await rows.ExecuteScalarAsync(cancellationToken) ?? 0) > 0)
                {
                    logger.LogInformation("{Database} already has operational rows; skipping restore.", testDatabase);
                    return;
                }
            }
        }

        var dump = Path.Combine(Path.GetTempPath(), "sila-me-backups", "sila_me_pre_five_migration.dump");
        if (!File.Exists(dump))
        {
            throw new InvalidOperationException("Legacy backup is missing; cannot copy sila_me into FIVE TEST.");
        }

        var psi = new ProcessStartInfo("pg_restore")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        var target = new NpgsqlConnectionStringBuilder(targetCs);
        psi.ArgumentList.Add("--no-owner");
        psi.ArgumentList.Add("--no-acl");
        psi.ArgumentList.Add("-h");
        psi.ArgumentList.Add(target.Host ?? "localhost");
        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add(target.Port.ToString());
        psi.ArgumentList.Add("-U");
        psi.ArgumentList.Add(target.Username ?? "sila");
        psi.ArgumentList.Add("-d");
        psi.ArgumentList.Add(target.Database);
        psi.ArgumentList.Add(dump);
        psi.Environment["PGPASSWORD"] = target.Password;
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("pg_restore could not start.");
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0 && process.ExitCode != 1)
        {
            throw new InvalidOperationException($"pg_restore failed ({process.ExitCode}): {error}");
        }

        logger.LogInformation("Restored sila_me backup into {Database}", testDatabase);
    }

    private static async Task ConsolidateFiveTestAsync(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        await ExecAsync(connection, tx, """DELETE FROM sessions;""", cancellationToken);
        await ExecAsync(connection, tx, """SAVEPOINT optional_cleanup;""", cancellationToken);
        try
        {
            await ExecAsync(connection, tx, """DELETE FROM idempotency_records;""", cancellationToken);
        }
        catch (PostgresException)
        {
            await ExecAsync(connection, tx, """ROLLBACK TO SAVEPOINT optional_cleanup;""", cancellationToken);
        }
        await ExecAsync(connection, tx, """
            UPDATE organizations
            SET "Code" = 'FIVE', "Name" = 'Five Hotels and Resorts', "UpdatedAt" = NOW()
            WHERE "Code" = 'TEST-PHASE2';
            """, cancellationToken);
        await ExecAsync(connection, tx, """
            UPDATE organizations
            SET "Code" = 'FIVE-JVC', "Name" = 'FIVE JVC', "UpdatedAt" = NOW()
            WHERE "Code" = 'SILA-DEMO';
            """, cancellationToken);

        Guid? fiveId = null;
        Guid? jvcId = null;
        await using (var findFive = new NpgsqlCommand("""SELECT "Id" FROM organizations WHERE "Code" = 'FIVE' LIMIT 1""", connection, tx))
        await using (var findJvc = new NpgsqlCommand("""SELECT "Id" FROM organizations WHERE "Code" = 'FIVE-JVC' LIMIT 1""", connection, tx))
        {
            fiveId = await findFive.ExecuteScalarAsync(cancellationToken) as Guid?;
            jvcId = await findJvc.ExecuteScalarAsync(cancellationToken) as Guid?;
            if (fiveId is not null && jvcId is not null && fiveId != jvcId)
            {
                await ExecAsync(connection, tx, $"""
                    UPDATE organizations SET "ParentOrganizationId" = '{fiveId}' WHERE "Id" = '{jvcId}';
                    """, cancellationToken);
            }
        }

        await tx.CommitAsync(cancellationToken);
        logger.LogInformation("FIVE TEST hierarchy mapped FIVE={FiveId} FIVE-JVC={JvcId}", fiveId, jvcId);

        try
        {
            await using var pruneTx = await connection.BeginTransactionAsync(cancellationToken);
            await ExecAsync(connection, pruneTx, """
                DELETE FROM users
                WHERE "NormalizedEmail" NOT IN (
                    'BALA@CHERVIC.IN',
                    'CLOUD-TEST@SILAME.LOCAL',
                    'MOBILE-TEST@SILAME.LOCAL',
                    'CLOUDADMIN@SILAME.LOCAL',
                    'MOBILEADMIN@SILAME.LOCAL')
                  AND "Id" NOT IN (SELECT "UserId" FROM user_organization_memberships)
                  AND "Id" NOT IN (SELECT "CreatedByUserId" FROM document_storage_connections WHERE "CreatedByUserId" IS NOT NULL);
                """, cancellationToken);
            await pruneTx.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception)
        {
            logger.LogWarning(exception, "FIVE TEST user prune skipped.");
        }
    }

    private static async Task<string?> MigrateAndSeedAsync(
        string operationalConnectionString,
        string databaseName,
        IPasswordService passwords,
        CancellationToken cancellationToken)
    {
        _ = passwords;
        var options = new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(TenantOperationalDatabase.Bind(operationalConnectionString, databaseName))
            .Options;
        await using var db = new SilaMeDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
        await AccessSeed.SeedCatalogAsync(db, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();
    }

    private static async Task EnsureFiveTestReceivingAccessAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new SilaMeDbContext(options);
        var organization = await db.Organizations.SingleOrDefaultAsync(item => item.Code == "FIVE", cancellationToken);
        var role = await db.Roles.SingleOrDefaultAsync(item => item.Key == "RECEIVING_OPERATOR", cancellationToken)
            ?? await db.Roles.SingleOrDefaultAsync(item => item.Key == "SUPER_ADMIN", cancellationToken);
        if (organization is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var email in new[] { "MOBILE-TEST@SILAME.LOCAL", "BALA@CHERVIC.IN" })
        {
            var user = await db.Users.SingleOrDefaultAsync(item => item.NormalizedEmail == email, cancellationToken);
            if (user is null)
            {
                continue;
            }

            if (!await db.UserOrganizationMemberships.AnyAsync(item => item.UserId == user.Id && item.OrganizationId == organization.Id, cancellationToken))
            {
                db.UserOrganizationMemberships.Add(new UserOrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    OrganizationId = organization.Id,
                    CreatedAt = now,
                });
            }

            if (role is not null &&
                !await db.UserRoleAssignments.AnyAsync(item => item.UserId == user.Id && item.RoleId == role.Id && item.OrganizationId == organization.Id, cancellationToken))
            {
                db.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id,
                    OrganizationId = organization.Id,
                    CreatedAt = now,
                });
            }

            if (!await db.UserApplicationAccess.AnyAsync(item => item.UserId == user.Id && item.Application == ApplicationKind.MOBILE, cancellationToken))
            {
                db.UserApplicationAccess.Add(new UserApplicationAccess
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Application = ApplicationKind.MOBILE,
                    CreatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureFiveTestReceivingMastersAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new SilaMeDbContext(options);
        var now = DateTime.UtcNow;
        var five = await db.Organizations.SingleOrDefaultAsync(item => item.Code == "FIVE", cancellationToken);
        if (five is null)
        {
            five = new Organization
            {
                Id = Guid.NewGuid(),
                Code = "FIVE",
                Name = TenantOperationalDatabase.FiveCustomerName,
                Kind = OrganizationKind.CUSTOMER,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Organizations.Add(five);
            await db.SaveChangesAsync(cancellationToken);
        }

        var jvc = await db.Organizations.SingleOrDefaultAsync(item => item.Code == "FIVE-JVC", cancellationToken);
        if (jvc is not null && jvc.ParentOrganizationId != five.Id)
        {
            jvc.ParentOrganizationId = five.Id;
            jvc.UpdatedAt = now;
        }

        var unit = await db.OrganizationUnits.SingleOrDefaultAsync(item => item.OrganizationId == five.Id && item.Code == "FIVE-MAIN", cancellationToken);
        if (unit is null)
        {
            unit = new OrganizationUnit
            {
                Id = Guid.NewGuid(),
                OrganizationId = five.Id,
                Code = "FIVE-MAIN",
                Name = "Five Main Store",
                Kind = OrganizationUnitKind.STORE,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.OrganizationUnits.Add(unit);
        }

        if (!await db.CompanyCodes.AnyAsync(item => item.OrganizationId == five.Id && item.CompanyCode == "1050", cancellationToken))
        {
            db.CompanyCodes.Add(new CompanyCodeMaster
            {
                Id = Guid.NewGuid(), OrganizationId = five.Id, CompanyCode = "1050", CompanyName = "FIVE JVC",
                Country = "AE", Currency = "AED", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
            });
        }

        if (!await db.Properties.AnyAsync(item => item.OrganizationId == five.Id && item.PropertyCode == "FIVE-JVC", cancellationToken))
        {
            db.Properties.Add(new PropertyMaster
            {
                Id = Guid.NewGuid(), OrganizationId = five.Id, PropertyCode = "FIVE-JVC", PropertyName = "FIVE JVC",
                Country = "AE", CompanyCode = "1050", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
            });
        }

        var testSbn = await db.Suppliers.SingleOrDefaultAsync(item => item.OrganizationId == five.Id && item.SupplierCode == "1003430", cancellationToken);
        if (testSbn is null)
        {
            testSbn = new Supplier
            {
                Id = Guid.NewGuid(), OrganizationId = five.Id, SupplierCode = "1003430", Name = "Test SBN",
                NormalizedName = "TEST SBN", LegalName = "Test SBN", Status = StatusKind.ACTIVE, SourceSystem = "TEST",
                EntityCode = "DEFAULT", CompanyCode = "1050", Currency = "AED", CreatedAt = now, UpdatedAt = now,
            };
            db.Suppliers.Add(testSbn);
        }

        var abcFood = await db.Suppliers.SingleOrDefaultAsync(item => item.OrganizationId == five.Id && item.SupplierCode == "1000234", cancellationToken);
        if (abcFood is null)
        {
            abcFood = new Supplier
            {
                Id = Guid.NewGuid(), OrganizationId = five.Id, SupplierCode = "1000234", Name = "ABC FOOD",
                NormalizedName = "ABC FOOD", LegalName = "ABC FOOD", Status = StatusKind.ACTIVE, SourceSystem = "TEST",
                EntityCode = "DEFAULT", CompanyCode = "1050", Currency = "AED", CreatedAt = now, UpdatedAt = now,
            };
            db.Suppliers.Add(abcFood);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (!await db.PurchaseOrders.AnyAsync(item => item.OrganizationId == five.Id && item.PoNumber == "4500003415", cancellationToken))
        {
            var po = new PurchaseOrder
            {
                Id = Guid.NewGuid(), OrganizationId = five.Id, OperatingUnitId = unit.Id, SupplierId = testSbn.Id,
                PoNumber = "4500003415", EntityCode = "1050", CompanyCode = "1050", ErpSupplierId = "1003430",
                SupplierName = "Test SBN", Currency = "AED", Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST",
                CreatedAt = now, UpdatedAt = now,
            };
            foreach (var line in new[] { 10, 20, 30, 40 })
            {
                po.Items.Add(new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = line, ItemNumber = line.ToString(),
                    MaterialCode = $"MAT-{line}", Description = $"Test SBN item {line}", OrderedQuantity = 5,
                    ReceivedQuantity = 0, OpenQuantity = 5, Uom = "EA", Status = PurchaseOrderItemStatus.OPEN,
                    GoodsReceiptExpected = true, Plant = "1050", CreatedAt = now, UpdatedAt = now,
                });
            }
            db.PurchaseOrders.Add(po);
        }

        if (!await db.PurchaseOrders.AnyAsync(item => item.OrganizationId == five.Id && item.PoNumber == "4500002849", cancellationToken))
        {
            db.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = Guid.NewGuid(), OrganizationId = five.Id, OperatingUnitId = unit.Id, SupplierId = abcFood.Id,
                PoNumber = "4500002849", EntityCode = "1050", CompanyCode = "1050", ErpSupplierId = "1000234",
                SupplierName = "ABC FOOD", Currency = "AED", Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST",
                SourceLastChangedAtRaw = "EP23721", CreatedAt = now, UpdatedAt = now,
                Items =
                [
                    new PurchaseOrderItem
                    {
                        Id = Guid.NewGuid(), LineNumber = 10, ItemNumber = "10", MaterialCode = "MAT001",
                        Description = "Test item", OrderedQuantity = 1, ReceivedQuantity = 0, OpenQuantity = 1,
                        Uom = "EA", Status = PurchaseOrderItemStatus.OPEN, GoodsReceiptExpected = true, Plant = "1050",
                        CreatedAt = now, UpdatedAt = now,
                    },
                ],
            });
        }

        var admin = await db.Users.SingleOrDefaultAsync(item => item.NormalizedEmail == "BALA@CHERVIC.IN", cancellationToken);
        var super = await db.Roles.SingleOrDefaultAsync(item => item.Key == "SUPER_ADMIN", cancellationToken);
        if (admin is not null &&
            !await db.UserOrganizationMemberships.AnyAsync(item => item.UserId == admin.Id && item.OrganizationId == five.Id, cancellationToken))
        {
            db.UserOrganizationMemberships.Add(new UserOrganizationMembership
            {
                Id = Guid.NewGuid(), UserId = admin.Id, OrganizationId = five.Id, CreatedAt = now,
            });
        }

        if (admin is not null && super is not null &&
            !await db.UserRoleAssignments.AnyAsync(item => item.UserId == admin.Id && item.RoleId == super.Id && item.OrganizationId == five.Id, cancellationToken))
        {
            db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(), UserId = admin.Id, RoleId = super.Id, OrganizationId = five.Id, CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await EnsureFiveTestIntegrationShellsAsync(db, five.Id, now, cancellationToken);
        await EnsureFiveTestInvoiceOcrPolicyAsync(db, five.Id, now, cancellationToken);
    }

    private static async Task EnsureFiveTestInvoiceOcrPolicyAsync(
        SilaMeDbContext db,
        Guid organizationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await db.InvoiceOcrConfigurations.AnyAsync(item => item.OrganizationId == organizationId, cancellationToken))
        {
            return;
        }

        var owner = await db.Users.SingleOrDefaultAsync(item => item.NormalizedEmail == "BALA@CHERVIC.IN", cancellationToken)
            ?? await db.Users.OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (owner is null)
        {
            return;
        }

        db.InvoiceOcrConfigurations.Add(new InvoiceOcrConfiguration
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CreatedByUserId = owner.Id,
            UpdatedByUserId = owner.Id,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureFiveTestIntegrationShellsAsync(
        SilaMeDbContext db,
        Guid organizationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!await db.ApiIntegrationConfigurations.AnyAsync(item => item.OrganizationId == organizationId && item.Name == "FIVE S4 - POST GRN", cancellationToken))
        {
            var sample = IntegrationDesignerCatalog.FiveS4PostGrnSample();
            var config = new ApiIntegrationConfiguration
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                EntityCode = "ALL",
                Name = sample.Name,
                Description = sample.Description,
                ProcessType = sample.ProcessType,
                Protocol = sample.Protocol,
                SystemKind = sample.SystemKind,
                BaseUrl = sample.BaseUrl,
                ServicePath = sample.ServicePath,
                EntitySet = sample.EntitySet,
                HttpMethod = sample.HttpMethod,
                AuthenticationType = sample.AuthenticationType,
                TimeoutSeconds = sample.TimeoutSeconds,
                RetryCount = sample.RetryCount,
                Priority = sample.Priority,
                Status = IntegrationConfigurationStatus.DRAFT,
                DesignerJson = System.Text.Json.JsonSerializer.Serialize(
                    sample.Designer with { ResponseMappings = sample.ResponseMappings },
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
                CreatedAt = now,
                UpdatedAt = now,
            };
            foreach (var mapping in sample.Mappings)
            {
                config.FieldMappings.Add(new ApiFieldMapping
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = config.Id,
                    SourceField = mapping.SourceField,
                    TargetField = mapping.TargetField,
                    SourceKind = mapping.SourceKind,
                    SourceStructure = mapping.SourceStructure,
                    IsCollection = mapping.IsCollection,
                    NullPolicy = mapping.NullPolicy,
                    UpdatedAt = now,
                });
            }
            db.ApiIntegrationConfigurations.Add(config);
            db.IntegrationRoutes.Add(new IntegrationRoute
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProcessType = IntegrationProcessType.POST_GRN,
                SystemKind = IntegrationSystemKind.SAP_S4HANA,
                ApiIntegrationConfigurationId = config.Id,
                AppliesToAllCompanyCodes = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        if (!await db.ApiIntegrationConfigurations.AnyAsync(item => item.OrganizationId == organizationId && item.Name == "FIVETEST-GRNPOSTING-ARIBA", cancellationToken))
        {
            var designer = IntegrationDesignerCatalog.DefaultsFor(IntegrationSystemKind.SAP_ARIBA, IntegrationProcessType.POST_GRN, IntegrationProtocol.SOAP) with
            {
                SoapAction = "ExternalReceiptImport",
                SoapPartition = AribaPostGrnAdapter.DefaultSoapPartition,
                SoapVariant = AribaPostGrnAdapter.DefaultSoapVariant,
            };
            var config = new ApiIntegrationConfiguration
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                EntityCode = "ALL",
                Name = "FIVETEST-GRNPOSTING-ARIBA",
                Description = "Ariba ExternalReceiptImport SOAP. Re-enter BASIC credentials.",
                ProcessType = IntegrationProcessType.POST_GRN,
                Protocol = IntegrationProtocol.SOAP,
                SystemKind = IntegrationSystemKind.SAP_ARIBA,
                BaseUrl = "https://s1.mn1.ariba.com/Buyer/soap/fiveholdings-C1-T/ExternalReceiptImport",
                HttpMethod = "POST",
                AuthenticationType = IntegrationAuthenticationType.BASIC,
                TimeoutSeconds = 30,
                RetryCount = 2,
                Priority = 90,
                Status = IntegrationConfigurationStatus.DRAFT,
                DesignerJson = System.Text.Json.JsonSerializer.Serialize(
                    designer,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.ApiIntegrationConfigurations.Add(config);
            db.IntegrationRoutes.Add(new IntegrationRoute
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProcessType = IntegrationProcessType.POST_GRN,
                SystemKind = IntegrationSystemKind.SAP_ARIBA,
                ApiIntegrationConfigurationId = config.Id,
                AppliesToAllCompanyCodes = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RecordMigrationAsync(
        PlatformDbContext platform,
        string tenantCode,
        TenantEnvironmentType type,
        string? migration,
        CancellationToken cancellationToken)
    {
        var environment = await platform.TenantEnvironments
            .Include(item => item.Tenant)
            .SingleOrDefaultAsync(item => item.Tenant.NormalizedTenantCode == tenantCode.ToUpperInvariant() && item.EnvironmentType == type, cancellationToken);
        if (environment is null)
        {
            return;
        }

        environment.LastAppliedMigration = migration;
        environment.ProvisioningCheckpoint = ProvisioningCheckpoint.READY;
        environment.UpdatedAt = DateTime.UtcNow;
        await platform.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureProductionIsCleanAsync(string connectionString, IPasswordService passwords, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new SilaMeDbContext(options);
        if (await db.Suppliers.AnyAsync(item => item.SupplierCode == "1003430", cancellationToken)
            || await db.PurchaseOrders.AnyAsync(item => item.PoNumber == "4500003415", cancellationToken))
        {
            throw new InvalidOperationException("FIVE PROD must not contain TEST receiving demo transactions.");
        }

        var now = DateTime.UtcNow;
        var organization = await db.Organizations.SingleOrDefaultAsync(item => item.Code == "FIVE", cancellationToken);
        if (organization is null)
        {
            organization = new Organization
            {
                Id = Guid.NewGuid(),
                Code = "FIVE",
                Name = TenantOperationalDatabase.FiveCustomerName,
                Kind = OrganizationKind.CUSTOMER,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Organizations.Add(organization);
        }
        else
        {
            organization.Name = TenantOperationalDatabase.FiveCustomerName;
        }

        var admin = await db.Users.SingleOrDefaultAsync(item => item.NormalizedEmail == "BALA@CHERVIC.IN", cancellationToken);
        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "bala@chervic.in",
                NormalizedEmail = "BALA@CHERVIC.IN",
                DisplayName = "Bala",
                PasswordHash = string.Empty,
                CreatedAt = now,
                UpdatedAt = now,
            };
            var password = Environment.GetEnvironmentVariable("SILA_ME_ADMIN_PASSWORD")
                ?? Environment.GetEnvironmentVariable("SILA_ME_PLATFORM_ADMIN_PASSWORD")
                ?? "ChangeMe123!";
            admin.PasswordHash = passwords.HashPassword(admin, password);
            db.Users.Add(admin);
        }

        await db.SaveChangesAsync(cancellationToken);
        if (!await db.UserOrganizationMemberships.AnyAsync(item => item.UserId == admin.Id && item.OrganizationId == organization.Id, cancellationToken))
        {
            db.UserOrganizationMemberships.Add(new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                OrganizationId = organization.Id,
                CreatedAt = now,
            });
        }

        var super = await db.Roles.SingleOrDefaultAsync(item => item.Key == "SUPER_ADMIN", cancellationToken);
        if (super is not null &&
            !await db.UserRoleAssignments.AnyAsync(item => item.UserId == admin.Id && item.RoleId == super.Id && item.OrganizationId == organization.Id, cancellationToken))
        {
            db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                RoleId = super.Id,
                OrganizationId = organization.Id,
                CreatedAt = now,
            });
        }

        foreach (var application in new[] { ApplicationKind.CLOUD, ApplicationKind.MOBILE })
        {
            if (!await db.UserApplicationAccess.AnyAsync(item => item.UserId == admin.Id && item.Application == application, cancellationToken))
            {
                db.UserApplicationAccess.Add(new UserApplicationAccess
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    Application = application,
                    CreatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RegisterFiveTenantAsync(
        PlatformDbContext platform,
        string testSecret,
        string prodSecret,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var occupied in await platform.TenantEnvironments
            .Where(item => item.RouteSlug == TenantOperationalDatabase.FiveCustomerRouteSlug
                || item.RouteSlug == TenantOperationalDatabase.FiveLegacyTestRouteAlias)
            .ToListAsync(cancellationToken))
        {
            var owner = await platform.Tenants.SingleAsync(item => item.Id == occupied.TenantId, cancellationToken);
            if (string.Equals(owner.NormalizedTenantCode, "FIVE", StringComparison.OrdinalIgnoreCase))
            {
                occupied.RouteSlug = null;
                continue;
            }

            occupied.RouteSlug = occupied.EnvironmentType == TenantEnvironmentType.PRODUCTION
                ? $"five-legacy-{owner.Id.ToString("N")[..8]}"
                : $"five-alias-legacy-{owner.Id.ToString("N")[..8]}";
        }

        var tenant = await platform.Tenants.Include(item => item.Environments)
            .SingleOrDefaultAsync(item => item.NormalizedTenantCode == "FIVE", cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantCode = TenantOperationalDatabase.FiveTenantCode,
                NormalizedTenantCode = "FIVE",
                CustomerName = TenantOperationalDatabase.FiveCustomerName,
                LegalName = TenantOperationalDatabase.FiveCustomerName,
                CountryCode = "AE",
                DefaultTimeZone = "Asia/Dubai",
                DefaultCurrency = "AED",
                Status = TenantStatus.ACTIVE,
                MaxCloudUsers = 100,
                MaxMobileUsers = 500,
                CreatedAt = now,
                UpdatedAt = now,
            };
            platform.Tenants.Add(tenant);
            await platform.SaveChangesAsync(cancellationToken);
        }
        else
        {
            tenant.TenantCode = TenantOperationalDatabase.FiveTenantCode;
            tenant.NormalizedTenantCode = "FIVE";
            tenant.CustomerName = TenantOperationalDatabase.FiveCustomerName;
            tenant.Status = TenantStatus.ACTIVE;
            tenant.UpdatedAt = now;
        }

        await EnsureEnvironmentAsync(platform, tenant, TenantEnvironmentType.TEST, TenantOperationalDatabase.FiveCustomerRouteSlug, testSecret, now, cancellationToken);
        await EnsureEnvironmentAsync(platform, tenant, TenantEnvironmentType.PRODUCTION, null, prodSecret, now, cancellationToken);
        await ArchiveObsoleteGeneratedTenantsAsync(platform, cancellationToken);
        if (!await platform.TenantProductEntitlements.AnyAsync(item => item.TenantId == tenant.Id && item.ProductCode == ProductCodes.SilaMe, cancellationToken))
        {
            platform.TenantProductEntitlements.Add(new TenantProductEntitlement
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                ProductCode = ProductCodes.SilaMe,
                Status = EntitlementStatus.ACTIVE,
                CreatedAt = now,
            });
        }
        else
        {
            var product = await platform.TenantProductEntitlements.SingleAsync(item => item.TenantId == tenant.Id && item.ProductCode == ProductCodes.SilaMe, cancellationToken);
            product.Status = EntitlementStatus.ACTIVE;
        }

        foreach (var module in ProductCodes.DefaultModules)
        {
            if (!await platform.TenantModuleEntitlements.AnyAsync(item => item.TenantId == tenant.Id && item.ModuleCode == module, cancellationToken))
            {
                platform.TenantModuleEntitlements.Add(new TenantModuleEntitlement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    ProductCode = ProductCodes.SilaMe,
                    ModuleCode = module,
                    Status = EntitlementStatus.ACTIVE,
                    CreatedAt = now,
                });
            }
        }

        await platform.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureEnvironmentAsync(
        PlatformDbContext platform,
        Tenant tenant,
        TenantEnvironmentType type,
        string? routeSlug,
        string secretReference,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var customerUrl = TenantOperationalDatabase.FiveCustomerBaseUrl();
        var environment = tenant.Environments.FirstOrDefault(item => item.EnvironmentType == type);
        if (environment is null)
        {
            environment = new TenantEnvironment
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                EnvironmentType = type,
                DisplayName = TenantOperationalDatabase.EnvironmentCode(type),
                Status = TenantEnvironmentStatus.ACTIVE,
                BaseUrl = type == TenantEnvironmentType.TEST ? customerUrl : TenantOperationalDatabase.CloudOrigin(),
                RouteSlug = routeSlug,
                DatabaseSecretReference = secretReference,
                DataRegion = "LOCAL",
                IsActive = true,
                ProvisioningCheckpoint = ProvisioningCheckpoint.READY,
                CreatedAt = now,
                UpdatedAt = now,
            };
            platform.TenantEnvironments.Add(environment);
        }
        else
        {
            environment.RouteSlug = routeSlug;
            environment.DatabaseSecretReference = secretReference;
            environment.DisplayName = TenantOperationalDatabase.EnvironmentCode(type);
            environment.BaseUrl = type == TenantEnvironmentType.TEST ? customerUrl : TenantOperationalDatabase.CloudOrigin();
            environment.Status = TenantEnvironmentStatus.ACTIVE;
            environment.IsActive = true;
            environment.ProvisioningCheckpoint = ProvisioningCheckpoint.READY;
            environment.UpdatedAt = now;
        }

        await Task.CompletedTask;
    }

    private static async Task ArchiveObsoleteGeneratedTenantsAsync(PlatformDbContext platform, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tenants = await platform.Tenants.Include(item => item.Environments).ToListAsync(cancellationToken);
        foreach (var tenant in tenants)
        {
            var code = tenant.NormalizedTenantCode ?? tenant.TenantCode;
            if (string.Equals(code, "FIVE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tenant.TenantCode, "SILA-DEV", StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, "SILA-DEV", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsObsoleteGeneratedTenant(code))
            {
                continue;
            }

            tenant.Status = TenantStatus.ARCHIVED;
            tenant.UpdatedAt = now;
            foreach (var environment in tenant.Environments)
            {
                environment.IsActive = false;
                environment.RouteSlug = null;
                environment.UpdatedAt = now;
            }
        }

        await platform.SaveChangesAsync(cancellationToken);
    }

    private static bool IsObsoleteGeneratedTenant(string code)
    {
        var normalized = code.Trim().ToUpperInvariant().Replace('_', '-');
        if (normalized.StartsWith("FIVE-TEST", StringComparison.Ordinal)
            || normalized.StartsWith("FIVP", StringComparison.Ordinal))
        {
            return true;
        }

        string[] prefixes = ["LIC", "CNS", "LNCH", "ISOA", "ISOB", "JOBA", "JOBB", "SUS", "ASG-"];
        return prefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static async Task ExecAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
