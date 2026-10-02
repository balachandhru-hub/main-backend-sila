using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class OperationalMasterExcelTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Company_plant_and_storage_excel_round_trip_is_idempotent_and_allows_empty_export()
    {
        var client = await AuthenticatedFiveAsync();
        using var scope = factory.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<SilaMe.Api.Tenancy.ISecretProvider>();
        await using var db = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(SilaMe.Api.Data.DatabaseUrl.Normalize(secrets.Resolve("five-test"))).Options);
        var organization = await db.Organizations.SingleAsync(item => item.Code == "FIVE");
        Assert.Equal("sila_five_test", new Npgsql.NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Database);

        foreach (var kind in new[] { "COMPANY_CODES", "PROPERTIES" })
        {
            var empty = await client.GetAsync($"/api/v1/master-data/records/export?organizationId={organization.Id}&kind={kind}");
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            Assert.Contains("spreadsheetml.sheet", empty.Content.Headers.ContentType?.MediaType);
            var emptyBytes = await empty.Content.ReadAsByteArrayAsync();
            Assert.True(IsXlsx(emptyBytes));
            Assert.Contains(SheetName(kind), SheetNames(emptyBytes));
            Assert.Contains("INSTRUCTIONS", SheetNames(emptyBytes));
        }

        var companyTemplate = await client.GetByteArrayAsync($"/api/v1/master-data/records/import/template?organizationId={organization.Id}&kind=COMPANY_CODES");
        Assert.True(IsXlsx(companyTemplate));
        var companyWorkbook = SilaMe.Api.Services.IntegrationService.CreateWorkbook(
        [
            ("COMPANY_CODES", ["CompanyCode", "CompanyName", "Country", "Currency"], [["RSTCC", "Restore Test Co", "AE", "AED"]]),
            ("INSTRUCTIONS", ["Field", "Required", "Notes"], [["CompanyCode", "YES", "Business key"]]),
        ]);
        var companyPreview = await Preview(client, organization.Id, "COMPANY_CODES", companyWorkbook);
        Assert.Equal(1, companyPreview.GetProperty("validRows").GetInt32());
        Assert.Equal("NEW", companyPreview.GetProperty("rows")[0].GetProperty("action").GetString());
        await Commit(client, organization.Id, "COMPANY_CODES", companyPreview);
        var second = await Preview(client, organization.Id, "COMPANY_CODES", companyWorkbook);
        Assert.Equal("UPDATE", second.GetProperty("rows")[0].GetProperty("action").GetString());
        await Commit(client, organization.Id, "COMPANY_CODES", second);
        Assert.Equal(1, await db.CompanyCodes.CountAsync(item => item.OrganizationId == organization.Id && item.CompanyCode == "RSTCC"));

        var sharedPreview = await Preview(client, organization.Id, "COMPANY_CODES", SharedStringWorkbook(
            "COMPANY_CODES",
            ["CompanyCode", "CompanyName", "Country", "Currency"],
            [["XLCC", "Excel Shared Co", "AE", "AED"]]));
        Assert.Equal(1, sharedPreview.GetProperty("validRows").GetInt32());
        Assert.Equal("XLCC", Value(sharedPreview, "COMPANYCODE") ?? Value(sharedPreview, "CompanyCode"));
        await Commit(client, organization.Id, "COMPANY_CODES", sharedPreview);
        Assert.True(await db.CompanyCodes.AnyAsync(item => item.OrganizationId == organization.Id && item.CompanyCode == "XLCC"));

        var numericPreview = await Preview(client, organization.Id, "COMPANY_CODES", NumericSharedWorkbook());
        Assert.Equal(1, numericPreview.GetProperty("validRows").GetInt32());
        Assert.Equal("1000", Value(numericPreview, "COMPANYCODE") ?? Value(numericPreview, "CompanyCode"));

        var propertyWorkbook = SilaMe.Api.Services.IntegrationService.CreateWorkbook(
        [
            ("PROPERTIES", ["PropertyCode", "PropertyName", "Country", "CompanyCode"], [["RSTPR", "Restore Property", "AE", "RSTCC"]]),
            ("INSTRUCTIONS", ["Field", "Required", "Notes"], [["PropertyCode", "YES", "Business key"]]),
        ]);
        await Commit(client, organization.Id, "PROPERTIES", await Preview(client, organization.Id, "PROPERTIES", propertyWorkbook));
        Assert.True(await db.Properties.AnyAsync(item => item.OrganizationId == organization.Id && item.PropertyCode == "RSTPR"));

        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/master-data/records/export?organizationId={organization.Id}&kind=PLANTS")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/master-data/records/export?organizationId={organization.Id}&kind=STORAGE_LOCATIONS")).StatusCode);

        await db.Properties.Where(item => item.OrganizationId == organization.Id && item.PropertyCode == "RSTPR").ExecuteDeleteAsync();
        await db.CompanyCodes.Where(item => item.OrganizationId == organization.Id && (item.CompanyCode == "RSTCC" || item.CompanyCode == "XLCC" || item.CompanyCode == "1000")).ExecuteDeleteAsync();
    }

    private async Task<HttpClient> AuthenticatedFiveAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = "cloud-test@silame.local", password = "test-password-strong" });
        if (!login.IsSuccessStatusCode)
        {
            login = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email = "mobile-test@silame.local", password = "test-password-strong" });
            login.EnsureSuccessStatusCode();
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    private static async Task<JsonElement> Preview(HttpClient client, Guid organizationId, string kind, byte[] workbook)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(workbook) { Headers = { ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") } }, "file", $"{kind.ToLowerInvariant()}.xlsx");
        var response = await client.PostAsync($"/api/v1/master-data/records/import/preview?organizationId={organizationId}&kind={kind}", content);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static async Task Commit(HttpClient client, Guid organizationId, string kind, JsonElement preview)
    {
        var rows = preview.GetProperty("rows").EnumerateArray()
            .Where(item => item.GetProperty("isValid").GetBoolean())
            .Select(item => item.GetProperty("values").EnumerateObject().ToDictionary(property => property.Name, property => property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString()))
            .ToList();
        var response = await client.PostAsJsonAsync($"/api/v1/master-data/records/import?organizationId={organizationId}&kind={kind}", new { rows });
        response.EnsureSuccessStatusCode();
    }

    private static string? Value(JsonElement preview, string key)
    {
        foreach (var property in preview.GetProperty("rows")[0].GetProperty("values").EnumerateObject())
        {
            if (string.Equals(property.Name.Replace("_", ""), key.Replace("_", ""), StringComparison.OrdinalIgnoreCase))
                return property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
        }
        return null;
    }

    private static byte[] SharedStringWorkbook(string sheet, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var strings = headers.Concat(rows.SelectMany(row => row)).ToList();
        var sst = string.Concat(strings.Select(value => $"<si><t>{System.Security.SecurityElement.Escape(value)}</t></si>"));
        string RowXml(int rowNumber, IReadOnlyList<string> values, int offset)
        {
            var cells = string.Concat(values.Select((_, index) =>
                $"<c r=\"{ColumnName(index + 1)}{rowNumber}\" t=\"s\"><v>{offset + index}</v></c>"));
            return $"<row r=\"{rowNumber}\">{cells}</row>";
        }
        var sheetXml = RowXml(1, headers, 0) + string.Concat(rows.Select((row, index) =>
            RowXml(index + 2, row, headers.Count + rows.Take(index).Sum(item => item.Count))));
        return Zip(
            ("[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/></Types>"""),
            ("_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>"""),
            ("xl/workbook.xml", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="{sheet}" sheetId="1" r:id="rId1"/></sheets></workbook>"""),
            ("xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings" Target="sharedStrings.xml"/></Relationships>"""),
            ("xl/sharedStrings.xml", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">{sst}</sst>"""),
            ("xl/worksheets/sheet1.xml", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>{sheetXml}</sheetData></worksheet>""")
        );
    }

    private static byte[] NumericSharedWorkbook() => Zip(
        ("[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/></Types>"""),
        ("_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>"""),
        ("xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="COMPANY_CODES" sheetId="1" r:id="rId1"/></sheets></workbook>"""),
        ("xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings" Target="sharedStrings.xml"/></Relationships>"""),
        ("xl/sharedStrings.xml", """<?xml version="1.0"?><sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><si><t>CompanyCode</t></si><si><t>CompanyName</t></si><si><t>Excel Numeric Co</t></si></sst>"""),
        ("xl/worksheets/sheet1.xml", """<?xml version="1.0"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c></row><row r="2"><c r="A2"><v>1000</v></c><c r="B2" t="s"><v>2</v></c></row></sheetData></worksheet>""")
    );

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (var (name, content) in entries)
            {
                using var stream = archive.CreateEntry(name).Open();
                using var writer = new StreamWriter(stream);
                writer.Write(content);
            }
        }
        return output.ToArray();
    }

    private static string ColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static bool IsXlsx(byte[] bytes)
    {
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        return archive.GetEntry("xl/workbook.xml") is not null;
    }

    private static IReadOnlyList<string> SheetNames(byte[] bytes)
    {
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var stream = archive.GetEntry("xl/workbook.xml")!.Open();
        var xml = System.Xml.Linq.XDocument.Load(stream);
        return xml.Descendants().Where(item => item.Name.LocalName == "sheet").Select(item => (string?)item.Attribute("name") ?? string.Empty).ToList();
    }

    private static string SheetName(string kind) => kind switch
    {
        "COMPANY_CODES" => "COMPANY_CODES",
        "PROPERTIES" => "PROPERTIES",
        "PLANTS" => "PLANTS",
        _ => "STORAGE_LOCATIONS",
    };
}
