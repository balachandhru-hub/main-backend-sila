using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class MasterRecordService(SilaMeDbContext db)
{
    public async Task<IReadOnlyList<CompanyCodeMasterResponse>> ListCompanyCodesAsync(Guid organizationId, string? query, string? status, CancellationToken cancellationToken)
    {
        var rows = db.CompanyCodes.AsNoTracking().Where(item => item.OrganizationId == organizationId && !item.IsDeleted);
        if (Enum.TryParse<StatusKind>(status, true, out var parsed)) rows = rows.Where(item => item.Status == parsed);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            rows = rows.Where(item => item.CompanyCode.Contains(term) || item.CompanyName.Contains(term));
        }
        return await rows.OrderBy(item => item.CompanyCode).Take(500).Select(item => ToCompany(item)).ToListAsync(cancellationToken);
    }

    public async Task<CompanyCodeMasterResponse> UpsertCompanyCodeAsync(Guid organizationId, Guid? id, CompanyCodeUpsertRequest request, CancellationToken cancellationToken)
    {
        var code = request.CompanyCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code)) throw new IntegrationException("COMPANY_CODE_REQUIRED", "Company code is required.");
        if (string.IsNullOrWhiteSpace(request.CompanyName)) throw new IntegrationException("COMPANY_NAME_REQUIRED", "Company name is required.");
        if (await db.CompanyCodes.AnyAsync(item => item.OrganizationId == organizationId && item.CompanyCode == code && item.Id != id, cancellationToken))
            throw new IntegrationException("DUPLICATE_COMPANY_CODE", "A company code with this value already exists.", 409);
        var row = id is null ? null : await db.CompanyCodes.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && row is null) throw new IntegrationException("COMPANY_CODE_NOT_FOUND", "The company code was not found.", 404);
        row ??= new CompanyCodeMaster { Id = Guid.NewGuid(), OrganizationId = organizationId, CompanyCode = code, CompanyName = request.CompanyName.Trim(), CreatedAt = DateTime.UtcNow };
        row.CompanyCode = code;
        row.CompanyName = request.CompanyName.Trim();
        row.Country = Trim(request.Country);
        row.Currency = Trim(request.Currency);
        row.Address = Trim(request.Address);
        row.City = Trim(request.City);
        row.TaxRegistrationNumber = Trim(request.TaxRegistrationNumber);
        row.SourceSystem = Trim(request.SourceSystem);
        row.ExternalId = Trim(request.ExternalId);
        row.Status = request.Status;
        row.IsDeleted = false;
        row.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.CompanyCodes.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToCompany(row);
    }

    public async Task<IReadOnlyList<PropertyMasterResponse>> ListPropertiesAsync(Guid organizationId, string? query, string? status, CancellationToken cancellationToken)
    {
        var rows = db.Properties.AsNoTracking().Where(item => item.OrganizationId == organizationId && !item.IsDeleted);
        if (Enum.TryParse<StatusKind>(status, true, out var parsed)) rows = rows.Where(item => item.Status == parsed);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            rows = rows.Where(item => item.PropertyCode.Contains(term) || item.PropertyName.Contains(term));
        }
        return await rows.OrderBy(item => item.PropertyCode).Take(500).Select(item => ToProperty(item)).ToListAsync(cancellationToken);
    }

    public async Task<PropertyMasterResponse> UpsertPropertyAsync(Guid organizationId, Guid? id, PropertyUpsertRequest request, CancellationToken cancellationToken)
    {
        var code = request.PropertyCode.Trim().ToUpperInvariant();
        if (await db.Properties.AnyAsync(item => item.OrganizationId == organizationId && item.PropertyCode == code && item.Id != id, cancellationToken))
            throw new IntegrationException("PROPERTY_EXISTS", "A property with this code already exists.", 409);
        var row = id is null ? null : await db.Properties.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && row is null) throw new IntegrationException("PROPERTY_NOT_FOUND", "The property was not found.", 404);
        row ??= new PropertyMaster { Id = Guid.NewGuid(), OrganizationId = organizationId, PropertyCode = code, PropertyName = request.PropertyName.Trim(), CreatedAt = DateTime.UtcNow };
        row.PropertyCode = code;
        row.PropertyName = request.PropertyName.Trim();
        row.Country = Trim(request.Country);
        row.CompanyCode = NormalizeCode(request.CompanyCode);
        row.Status = request.Status;
        row.IsDeleted = false;
        row.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.Properties.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToProperty(row);
    }

    public byte[] Template(OperationalMasterKind kind)
    {
        RejectRemovedMasters(kind);
        return BuildWorkbook(kind, []);
    }

    public async Task SetStatusAsync(Guid organizationId, OperationalMasterKind kind, Guid id, StatusKind status, CancellationToken cancellationToken)
    {
        RejectRemovedMasters(kind);
        var now = DateTime.UtcNow;
        switch (kind)
        {
            case OperationalMasterKind.COMPANY_CODES:
                var company = await db.CompanyCodes.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
                    ?? throw new IntegrationException("COMPANY_CODE_NOT_FOUND", "The company code was not found.", 404);
                company.Status = status; company.UpdatedAt = now; break;
            default:
                var property = await db.Properties.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
                    ?? throw new IntegrationException("PROPERTY_NOT_FOUND", "The property was not found.", 404);
                property.Status = status; property.UpdatedAt = now; break;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid organizationId, OperationalMasterKind kind, Guid id, CancellationToken cancellationToken)
    {
        RejectRemovedMasters(kind);
        await SetStatusAsync(organizationId, kind, id, StatusKind.INACTIVE, cancellationToken);
        switch (kind)
        {
            case OperationalMasterKind.COMPANY_CODES:
                (await db.CompanyCodes.SingleAsync(item => item.Id == id, cancellationToken)).IsDeleted = true; break;
            default:
                (await db.Properties.SingleAsync(item => item.Id == id, cancellationToken)).IsDeleted = true; break;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> ExportAsync(Guid organizationId, OperationalMasterKind kind, CancellationToken cancellationToken)
    {
        RejectRemovedMasters(kind);
        var rows = kind switch
        {
            OperationalMasterKind.COMPANY_CODES => (await db.CompanyCodes.AsNoTracking().Where(item => item.OrganizationId == organizationId && !item.IsDeleted).OrderBy(item => item.CompanyCode).ToListAsync(cancellationToken))
                .Select(ExportCompany).ToList(),
            _ => (await db.Properties.AsNoTracking().Where(item => item.OrganizationId == organizationId && !item.IsDeleted).OrderBy(item => item.PropertyCode).ToListAsync(cancellationToken))
                .Select(item => new List<string?> { item.PropertyCode, item.PropertyName, item.Country, item.CompanyCode, item.Status.ToString() }).ToList(),
        };
        return BuildWorkbook(kind, rows);
    }

    public async Task<IntegrationImportPreviewResponse> PreviewAsync(Guid organizationId, OperationalMasterKind kind, Stream file, string fileName, CancellationToken cancellationToken)
    {
        RejectRemovedMasters(kind);
        var parsed = await ReadRowsAsync(file, fileName, kind, cancellationToken);
        var existing = await ExistingKeysAsync(organizationId, kind, cancellationToken);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<IntegrationImportRowResponse>();
        for (var index = 0; index < parsed.Count; index++)
        {
            var values = Canonicalize(kind, parsed[index]);
            var (errors, codes) = await ValidateAsync(organizationId, kind, values, cancellationToken);
            var key = Key(kind, values);
            if (!string.IsNullOrWhiteSpace(key) && !seen.Add(key))
            {
                errors.Add("Duplicate business key in this workbook.");
                codes.Add(kind switch
                {
                    OperationalMasterKind.COMPANY_CODES => "DUPLICATE_COMPANY_CODE",
                    _ => "DUPLICATE_PROPERTY",
                });
            }
            var action = !string.IsNullOrWhiteSpace(key) && existing.Contains(key) ? "UPDATE" : "NEW";
            rows.Add(new IntegrationImportRowResponse(index + 2, errors.Count == 0, values, errors, codes, action));
        }
        return new IntegrationImportPreviewResponse(Guid.Empty, kind.ToString(), fileName, rows.Count,
            rows.Count(item => item.IsValid), rows.Count(item => !item.IsValid), Columns(kind), rows,
            rows.Count(item => item.IsValid && item.Action == "NEW"),
            rows.Count(item => item.IsValid && item.Action == "UPDATE"), 0, null);
    }

    public async Task<IntegrationImportCommitResponse> CommitAsync(Guid organizationId, OperationalMasterKind kind, IReadOnlyList<Dictionary<string, string?>> rows, CancellationToken cancellationToken)
    {
        RejectRemovedMasters(kind);
        if (rows.Count == 0) throw new IntegrationException("IMPORT_EMPTY", "There are no rows to commit.");
        var created = 0;
        var updated = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var raw in rows)
            {
                var values = Canonicalize(kind, raw);
                var (errors, _) = await ValidateAsync(organizationId, kind, values, cancellationToken);
                if (errors.Count > 0) throw new IntegrationException("IMPORT_VALIDATION_FAILED", "The spreadsheet contains invalid rows. No records were changed.");
                if (await UpsertFromRowAsync(organizationId, kind, values, cancellationToken)) created++; else updated++;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        return new IntegrationImportCommitResponse(
            new IntegrationExecutionResponse(Guid.Empty, Guid.Empty, IntegrationExecutionTrigger.EXCEL_IMPORT, IntegrationExecutionStatus.SUCCESS, DateTime.UtcNow, DateTime.UtcNow, rows.Count, created, updated, 0, null, null, null, null),
            rows.Count);
    }

    private async Task<bool> UpsertFromRowAsync(Guid organizationId, OperationalMasterKind kind, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken)
    {
        var status = ParseStatus(Get(values, "ISACTIVE") ?? Get(values, "STATUS"));
        return kind switch
        {
            OperationalMasterKind.COMPANY_CODES => await UpsertCompanyFromImport(organizationId, values, status, cancellationToken),
            _ => await UpsertPropertyFromImport(organizationId, values, status, cancellationToken),
        };
    }

    private async Task<bool> UpsertCompanyFromImport(Guid organizationId, IReadOnlyDictionary<string, string?> values, StatusKind status, CancellationToken cancellationToken)
    {
        var code = Get(values, "COMPANYCODE")!.Trim().ToUpperInvariant();
        var row = await db.CompanyCodes.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.CompanyCode == code, cancellationToken);
        var created = row is null;
        row ??= new CompanyCodeMaster { Id = Guid.NewGuid(), OrganizationId = organizationId, CompanyCode = code, CompanyName = Get(values, "COMPANYNAME")!.Trim(), CreatedAt = DateTime.UtcNow };
        row.CompanyName = Get(values, "COMPANYNAME")!.Trim();
        row.Country = Trim(Get(values, "COUNTRY"));
        row.Currency = Trim(Get(values, "CURRENCY"));
        row.Address = Trim(Get(values, "ADDRESS"));
        row.City = Trim(Get(values, "CITY"));
        row.TaxRegistrationNumber = Trim(Get(values, "TAXREGISTRATIONNUMBER"));
        row.SourceSystem = Trim(Get(values, "SOURCESYSTEM"));
        row.ExternalId = Trim(Get(values, "EXTERNALID"));
        row.Status = status;
        row.IsDeleted = false;
        row.UpdatedAt = DateTime.UtcNow;
        if (created) db.CompanyCodes.Add(row);
        return created;
    }

    private async Task<bool> UpsertPropertyFromImport(Guid organizationId, IReadOnlyDictionary<string, string?> values, StatusKind status, CancellationToken cancellationToken)
    {
        var code = Get(values, "PROPERTYCODE")!.Trim().ToUpperInvariant();
        var row = await db.Properties.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.PropertyCode == code, cancellationToken);
        var created = row is null;
        row ??= new PropertyMaster { Id = Guid.NewGuid(), OrganizationId = organizationId, PropertyCode = code, PropertyName = Get(values, "PROPERTYNAME")!.Trim(), CreatedAt = DateTime.UtcNow };
        row.PropertyName = Get(values, "PROPERTYNAME")!.Trim();
        row.Country = Trim(Get(values, "COUNTRY"));
        row.CompanyCode = NormalizeCode(Get(values, "COMPANYCODE"));
        row.Status = status;
        row.IsDeleted = false;
        row.UpdatedAt = DateTime.UtcNow;
        if (created) db.Properties.Add(row);
        return created;
    }

    private async Task EnsureCompanyExists(Guid organizationId, string? companyCode, bool require, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(companyCode);
        if (string.IsNullOrWhiteSpace(code))
        {
            if (require) throw new IntegrationException("COMPANY_CODE_REQUIRED", "Company code is required.");
            return;
        }
        if (!await db.CompanyCodes.AnyAsync(item => item.OrganizationId == organizationId && item.CompanyCode == code && !item.IsDeleted, cancellationToken))
            throw new IntegrationException("COMPANY_CODE_NOT_FOUND", "The referenced company code does not exist.");
    }

    private async Task<HashSet<string>> ExistingKeysAsync(Guid organizationId, OperationalMasterKind kind, CancellationToken cancellationToken) => kind switch
    {
        OperationalMasterKind.COMPANY_CODES => (await db.CompanyCodes.AsNoTracking().Where(item => item.OrganizationId == organizationId).Select(item => item.CompanyCode).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase),
        _ => (await db.Properties.AsNoTracking().Where(item => item.OrganizationId == organizationId).Select(item => item.PropertyCode).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase),
    };

    private static IReadOnlyList<string> Columns(OperationalMasterKind kind) => kind switch
    {
        OperationalMasterKind.COMPANY_CODES => ["CompanyCode", "CompanyName", "Country", "Currency", "IsActive", "Address", "City", "TaxRegistrationNumber", "SourceSystem", "ExternalId"],
        _ => ["PropertyCode", "PropertyName", "Country", "CompanyCode", "IsActive"],
    };

    private static string SheetName(OperationalMasterKind kind) => kind == OperationalMasterKind.COMPANY_CODES ? "COMPANY_CODES" : "PROPERTIES";

    private static string Key(OperationalMasterKind kind, IReadOnlyDictionary<string, string?> values) => kind switch
    {
        OperationalMasterKind.COMPANY_CODES => Get(values, "COMPANYCODE")?.Trim().ToUpperInvariant() ?? string.Empty,
        _ => Get(values, "PROPERTYCODE")?.Trim().ToUpperInvariant() ?? string.Empty,
    };

    private async Task<(List<string> Errors, List<string> Codes)> ValidateAsync(Guid organizationId, OperationalMasterKind kind, IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var codes = new List<string>();
        void Require(string column, string code, string message)
        {
            if (!string.IsNullOrWhiteSpace(Get(values, column))) return;
            errors.Add(message);
            codes.Add(code);
        }
        if (kind == OperationalMasterKind.COMPANY_CODES)
        {
            Require("COMPANYCODE", "COMPANY_CODE_REQUIRED", "Company code is required.");
            Require("COMPANYNAME", "COMPANY_NAME_REQUIRED", "Company name is required.");
        }
        else
        {
            Require("PROPERTYCODE", "PROPERTY_CODE_REQUIRED", "Property code is required.");
            Require("PROPERTYNAME", "PROPERTY_NAME_REQUIRED", "Property name is required.");
        }
        await Task.CompletedTask;
        return (errors, codes);
    }

    private static Dictionary<string, string?> Canonicalize(OperationalMasterKind kind, IReadOnlyDictionary<string, string?> source)
    {
        var lookup = source.ToDictionary(item => NormalizeHeader(item.Key), item => item.Value, StringComparer.OrdinalIgnoreCase);
        string? Read(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (lookup.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            return null;
        }
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in Columns(kind))
        {
            var aliases = column.ToUpperInvariant() switch
            {
                "PLANT" => new[] { "PLANT", "PLANTCODE" },
                "STORAGELOCATION" => new[] { "STORAGELOCATION", "STORAGELOCATIONCODE" },
                "PROPERTY" => new[] { "PROPERTY", "PROPERTYCODE" },
                "ISACTIVE" => new[] { "ISACTIVE", "STATUS" },
                _ => new[] { NormalizeHeader(column) },
            };
            result[NormalizeHeader(column)] = Read(aliases);
        }
        return result;
    }

    private static void RejectRemovedMasters(OperationalMasterKind kind)
    {
        if (kind is OperationalMasterKind.PLANTS or OperationalMasterKind.STORAGE_LOCATIONS)
            throw new IntegrationException("MASTER_REMOVED", "Plant and Storage Location masters were removed. Use Location Master.", 410);
    }

    private static string NormalizeHeader(string value) => Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", "");
    private static string? Get(IReadOnlyDictionary<string, string?> values, string key) =>
        values.TryGetValue(NormalizeHeader(key), out var value) ? value : null;
    private static StatusKind ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return StatusKind.ACTIVE;
        if (bool.TryParse(value, out var flag)) return flag ? StatusKind.ACTIVE : StatusKind.INACTIVE;
        if (value.Equals("NO", StringComparison.OrdinalIgnoreCase) || value.Equals("N", StringComparison.OrdinalIgnoreCase) || value.Equals("0")) return StatusKind.INACTIVE;
        return Enum.TryParse<StatusKind>(value, true, out var status) ? status : StatusKind.ACTIVE;
    }
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static CompanyCodeMasterResponse ToCompany(CompanyCodeMaster item) =>
        new(item.Id, item.CompanyCode, item.CompanyName, item.Country, item.Currency, item.Address, item.City, item.TaxRegistrationNumber, item.SourceSystem, item.ExternalId, item.Status, item.IsDeleted, item.UpdatedAt, item.Status == StatusKind.ACTIVE);
    private static PropertyMasterResponse ToProperty(PropertyMaster item) =>
        new(item.Id, item.PropertyCode, item.PropertyName, item.Country, item.CompanyCode, item.Status, item.IsDeleted, item.UpdatedAt, item.Status == StatusKind.ACTIVE);

    private static List<string?> ExportCompany(CompanyCodeMaster item) =>
        [item.CompanyCode, item.CompanyName, item.Country, item.Currency, item.Status == StatusKind.ACTIVE ? "TRUE" : "FALSE", item.Address, item.City, item.TaxRegistrationNumber, item.SourceSystem, item.ExternalId];

    private static byte[] BuildWorkbook(OperationalMasterKind kind, IReadOnlyList<List<string?>> rows)
    {
        var instructions = kind switch
        {
            OperationalMasterKind.COMPANY_CODES => new IReadOnlyList<string>[]
            {
                ["CompanyCode", "YES", "Business key within the customer. TenantId is never a column."],
                ["CompanyName", "YES", "Display name."],
                ["Country / Currency", "NO", "Do not invent defaults when blank."],
            },
            _ => new IReadOnlyList<string>[] { ["PropertyCode", "YES", "Unique property code within the customer."] },
        };
        return IntegrationService.CreateWorkbook(
        [
            (SheetName(kind), Columns(kind), rows.Select(row => (IReadOnlyList<string>)row.Select(item => item ?? string.Empty).ToList()).ToList()),
            ("INSTRUCTIONS", ["Field", "Required", "Notes"], instructions),
        ]);
    }

    private static async Task<List<Dictionary<string, string?>>> ReadRowsAsync(Stream file, string fileName, OperationalMasterKind kind, CancellationToken cancellationToken)
    {
        _ = fileName;
        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        if (memory.Length == 0) throw new IntegrationException("IMPORT_EMPTY", "The spreadsheet does not contain any data rows.");
        try
        {
            using var archiveStream = new MemoryStream(memory.ToArray());
            var sheets = ExcelOpenXml.ReadSheets(archiveStream);
            var wanted = SheetName(kind);
            var sheet = sheets.FirstOrDefault(item => string.Equals(item.Name, wanted, StringComparison.OrdinalIgnoreCase))
                ?? sheets.FirstOrDefault(item => !string.Equals(item.Name, "INSTRUCTIONS", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException();
            if (sheet.Rows.Count == 0) throw new IntegrationException("IMPORT_FORMAT_INVALID", "The upload is not a valid .xlsx spreadsheet.");
            return ExcelOpenXml.ToDictionaries(sheet.Rows).ToList();
        }
        catch (IntegrationException) { throw; }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or XmlException or InvalidOperationException)
        {
            throw new IntegrationException("IMPORT_FORMAT_INVALID", "The upload is not a valid .xlsx spreadsheet.");
        }
    }
}
