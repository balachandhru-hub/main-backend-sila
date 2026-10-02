using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class RecipeReferenceDataService(SilaMeDbContext db)
{
    public async Task<IReadOnlyList<UomMasterRow>> ListUomsAsync(Guid organizationId, string? status, CancellationToken cancellationToken)
    {
        await EnsureUomsAsync(organizationId, cancellationToken);
        var rows = db.UomMasters.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (Enum.TryParse<StatusKind>(status, true, out var parsed))
            rows = rows.Where(item => item.Status == parsed);
        return await rows.OrderBy(item => item.Code)
            .Select(item => new UomMasterRow(item.Id, item.Code, item.Name, item.Dimension.ToString(), item.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<UomMasterRow> UpsertUomAsync(Guid organizationId, Guid? id, UomMasterUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new RecipeManagementException("UOM_REQUIRED", "UOM code and name are required.");
        var code = RecipeUom.Normalize(request.Code);
        var row = id is null ? null : await db.UomMasters.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && row is null) throw new RecipeManagementException("UOM_NOT_FOUND", "The UOM was not found.", 404);
        var now = DateTime.UtcNow;
        row ??= new UomMaster { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = code, Name = request.Name.Trim(), CreatedAt = now };
        if (id is null) db.UomMasters.Add(row);
        row.Code = code;
        row.Name = request.Name.Trim();
        row.Dimension = Enum.TryParse<UomDimension>(request.Dimension, true, out var dimension) ? dimension : UomDimension.OTHER;
        row.Status = Enum.TryParse<StatusKind>(request.Status, true, out var parsed) ? parsed : StatusKind.ACTIVE;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new UomMasterRow(row.Id, row.Code, row.Name, row.Dimension.ToString(), row.Status.ToString());
    }

    public async Task<PageResult<MaterialUomConversionRow>> ListConversionsAsync(Guid organizationId, string? query, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        var size = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100);
        var rows = db.MaterialUomConversions.AsNoTracking().Include(item => item.Material)
            .Where(item => item.Material.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToUpperInvariant();
            rows = rows.Where(item => item.Material.MaterialCode.ToUpper().Contains(term) || item.Material.Description.ToUpper().Contains(term)
                || item.FromUom.ToUpper().Contains(term) || item.ToUom.ToUpper().Contains(term));
        }
        if (Guid.TryParse(cursor, out var after))
            rows = rows.Where(item => item.Id.CompareTo(after) > 0);
        var page = await rows.OrderBy(item => item.Id).Take(size + 1).ToListAsync(cancellationToken);
        var items = page.Take(size).Select(item => new MaterialUomConversionRow(item.Id, item.MaterialId, item.Material.MaterialCode, item.FromUom, item.ToUom,
            item.Numerator, item.Denominator, item.PackSize, item.PackUom, item.Source, item.IsActive)).ToList();
        return new PageResult<MaterialUomConversionRow>(items, page.Count > size ? items[^1].Id.ToString() : null, size, items.Count);
    }

    public async Task<MaterialUomConversionRow> UpsertConversionAsync(Guid organizationId, Guid? id, MaterialUomConversionUpsertRequest request, CancellationToken cancellationToken)
    {
        if (request.Numerator <= 0 || request.Denominator <= 0)
            throw new RecipeManagementException("CONVERSION_FACTOR_REQUIRED", "Numerator and denominator must be greater than zero.");
        var material = await db.Materials.SingleOrDefaultAsync(item => item.Id == request.MaterialId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        var row = id is null ? null : await db.MaterialUomConversions.SingleOrDefaultAsync(item => item.Id == id && item.MaterialId == material.Id, cancellationToken);
        if (id is not null && row is null) throw new RecipeManagementException("CONVERSION_NOT_FOUND", "The UOM conversion was not found.", 404);
        var now = DateTime.UtcNow;
        row ??= new MaterialUomConversion { Id = Guid.NewGuid(), MaterialId = material.Id, FromUom = "EA", ToUom = "EA", CreatedAt = now };
        if (id is null) db.MaterialUomConversions.Add(row);
        row.FromUom = RecipeUom.Normalize(request.FromUom);
        row.ToUom = RecipeUom.Normalize(request.ToUom);
        row.Numerator = request.Numerator;
        row.Denominator = request.Denominator;
        row.PackSize = request.PackSize;
        row.PackUom = string.IsNullOrWhiteSpace(request.PackUom) ? null : RecipeUom.Normalize(request.PackUom);
        row.Source = "MANUAL";
        row.IsActive = request.IsActive ?? true;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new MaterialUomConversionRow(row.Id, row.MaterialId, material.MaterialCode, row.FromUom, row.ToUom, row.Numerator, row.Denominator, row.PackSize, row.PackUom, row.Source, row.IsActive);
    }

    public async Task EnsureUomsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (await db.UomMasters.AnyAsync(item => item.OrganizationId == organizationId, cancellationToken)) return;
        var now = DateTime.UtcNow;
        foreach (var item in RecipeUom.Defaults)
        {
            db.UomMasters.Add(new UomMaster
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, Code = item.Code, Name = item.Name,
                Dimension = item.Dimension, Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
