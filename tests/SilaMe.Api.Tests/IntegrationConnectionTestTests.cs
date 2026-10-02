using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class IntegrationConnectionTestTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Connection_test_covers_auth_strategies_safety_and_validated_save()
    {
        factory.IntegrationHandler.Reset();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var designer = scope.ServiceProvider.GetRequiredService<IntegrationDesignerService>();
        var connections = scope.ServiceProvider.GetRequiredService<IntegrationConnectionTestService>();
        var orgA = await CreateOrgAsync(db, "CONN-A");
        var orgB = await CreateOrgAsync(db, "CONN-B");
        var userA = Guid.NewGuid();

        var s4 = IntegrationDesignerCatalog.FiveS4PostGrnSample();
        s4.Username = "erp-user";
        s4.Password = "erp-password";
        EnqueueS4Success();
        var s4Test = await connections.TestConnectionAsync(orgA, null, userA, s4, CancellationToken.None);
        Assert.True(s4Test.Success);
        Assert.NotNull(s4Test.TestId);
        Assert.Equal("CONNECTION_SUCCESSFUL", s4Test.Status);
        Assert.Contains(s4Test.Checks, item => item.Name == "CSRF" && item.Success);
        Assert.Contains(s4Test.Checks, item => item.Name == "ENTITY SET" && item.Success);
        Assert.Contains(factory.IntegrationHandler.Requests, uri =>
            uri.AbsoluteUri.TrimEnd('/') == "https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV");
        Assert.DoesNotContain("erp-password", JsonSerializer.Serialize(s4Test));
        Assert.DoesNotContain("secret-csrf", JsonSerializer.Serialize(s4Test));
        Assert.DoesNotContain(factory.IntegrationHandler.Methods, method => method is "POST" or "PUT" or "PATCH" or "DELETE");
        Assert.DoesNotContain(factory.IntegrationHandler.Requests, uri => uri.AbsolutePath.Contains("A_MaterialDocumentHeader", StringComparison.OrdinalIgnoreCase) && !uri.AbsolutePath.Contains("$metadata"));

        s4.TestId = s4Test.TestId;
        await connections.EnsureValidProofAsync(orgA, userA, s4, null, CancellationToken.None);
        var saved = await designer.SaveValidatedAsync(orgA, null, s4, userA.ToString(), CancellationToken.None);
        Assert.Equal(IntegrationConfigurationStatus.VALIDATED, saved.Status);
        Assert.Equal("VALIDATED", saved.ValidationStatus);
        Assert.Equal("SUCCESS", saved.ConnectionStatus);
        var stored = await db.ApiIntegrationConfigurations.SingleAsync(item => item.Id == saved.Id);
        Assert.DoesNotContain("erp-password", stored.ProtectedPassword);
        Assert.DoesNotContain("secret-csrf", stored.DesignerJson ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Null(stored.DesignerJson is null ? null : JsonDocument.Parse(stored.DesignerJson).RootElement.TryGetProperty("csrfToken", out _) ? "present" : null);

        s4.BaseUrl = "https://changed.example";
        var changed = await Assert.ThrowsAsync<IntegrationException>(() => connections.EnsureValidProofAsync(orgA, userA, s4, saved.Id, CancellationToken.None));
        Assert.Equal("CONFIGURATION_CHANGED_RETEST_REQUIRED", changed.Code);
        s4.BaseUrl = "https://my419951-api.s4hana.cloud.sap";
        s4.Username = "other-user";
        Assert.Equal("CONFIGURATION_CHANGED_RETEST_REQUIRED", (await Assert.ThrowsAsync<IntegrationException>(() => connections.EnsureValidProofAsync(orgA, userA, s4, saved.Id, CancellationToken.None))).Code);
        s4.Username = "erp-user";
        s4.Password = "other-secret";
        Assert.Equal("CONFIGURATION_CHANGED_RETEST_REQUIRED", (await Assert.ThrowsAsync<IntegrationException>(() => connections.EnsureValidProofAsync(orgA, userA, s4, saved.Id, CancellationToken.None))).Code);
        s4.Password = "erp-password";

        var expired = await db.IntegrationConnectionTests.SingleAsync(item => item.Id == s4Test.TestId);
        expired.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        Assert.Equal("TEST_EXPIRED", (await Assert.ThrowsAsync<IntegrationException>(() => connections.EnsureValidProofAsync(orgA, userA, s4, saved.Id, CancellationToken.None))).Code);
        expired.ExpiresAt = DateTime.UtcNow.AddMinutes(20);
        await db.SaveChangesAsync();
        Assert.Equal("TEST_TENANT_MISMATCH", (await Assert.ThrowsAsync<IntegrationException>(() => connections.EnsureValidProofAsync(orgB, userA, s4, null, CancellationToken.None))).Code);

        factory.IntegrationHandler.Enqueue(HttpStatusCode.Unauthorized, "no");
        var failedAuth = await connections.TestConnectionAsync(orgA, null, userA, s4, CancellationToken.None);
        Assert.False(failedAuth.Success);
        Assert.Null(failedAuth.TestId);
        Assert.Equal("AUTHENTICATION_FAILED", failedAuth.ErrorCode);
        s4.TestId = Guid.NewGuid();
        await Assert.ThrowsAsync<IntegrationException>(() => connections.EnsureValidProofAsync(orgA, userA, s4, null, CancellationToken.None));

        var tokenDraft = Base("Ariba REST", IntegrationSystemKind.SAP_ARIBA, IntegrationProcessType.GET_PO, IntegrationProtocol.REST, IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT);
        tokenDraft.TokenEndpoint = "https://api.ariba.com/v2/oauth/token";
        tokenDraft.ApiKey = "ariba-secret";
        tokenDraft.TokenBody = new Dictionary<string, string> { ["grant_type"] = "openapi_2lo" };
        tokenDraft.Designer = tokenDraft.Designer with { TokenHttpMethod = "POST", TokenResponsePath = "access_token", SelectedSourceStructures = ["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"access_token":"secret-token","token_type":"Bearer"}""");
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        var tokenOk = await connections.TestConnectionAsync(orgA, null, userA, tokenDraft, CancellationToken.None);
        Assert.True(tokenOk.Success);
        Assert.Contains(tokenOk.Checks, item => item.Name == "TOKEN" && item.Success);
        Assert.DoesNotContain("secret-token", JsonSerializer.Serialize(tokenOk));
        Assert.Contains(factory.IntegrationHandler.Methods, method => method == "POST");
        Assert.Contains(factory.IntegrationHandler.Requests, uri => uri.AbsolutePath.Contains("oauth", StringComparison.OrdinalIgnoreCase));

        factory.IntegrationHandler.Enqueue(HttpStatusCode.BadGateway, """{"error":"no"}""");
        var tokenFail = await connections.TestConnectionAsync(orgA, null, userA, tokenDraft, CancellationToken.None);
        Assert.False(tokenFail.Success);
        Assert.Equal("TOKEN_ENDPOINT_FAILED", tokenFail.ErrorCode);

        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"token":"x"}""");
        var missingPath = await connections.TestConnectionAsync(orgA, null, userA, tokenDraft, CancellationToken.None);
        Assert.Equal("TOKEN_EXTRACTION_FAILED", missingPath.ErrorCode);

        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"access_token":"secret-token"}""");
        factory.IntegrationHandler.Enqueue(HttpStatusCode.Forbidden, "no");
        var rejected = await connections.TestConnectionAsync(orgA, null, userA, tokenDraft, CancellationToken.None);
        Assert.Equal("TOKEN_REJECTED", rejected.ErrorCode);

        tokenDraft.TestId = tokenOk.TestId;
        tokenDraft.TokenEndpoint = "https://api.ariba.com/v2/oauth/other";
        Assert.Equal("CONFIGURATION_CHANGED_RETEST_REQUIRED", (await Assert.ThrowsAsync<IntegrationException>(() => connections.EnsureValidProofAsync(orgA, userA, tokenDraft, null, CancellationToken.None))).Code);
        tokenDraft.TokenEndpoint = "https://api.ariba.com/v2/oauth/token";

        var oauth = Base("Oracle", IntegrationSystemKind.ORACLE, IntegrationProcessType.GET_SUPPLIER, IntegrationProtocol.REST, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS);
        oauth.TokenEndpoint = "https://oracle.example/oauth/token";
        oauth.ClientId = "id";
        oauth.ClientSecret = "oracle-secret";
        oauth.Designer = oauth.Designer with { SelectedSourceStructures = ["SUPPLIER"] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"access_token":"oracle-token","token_type":"Bearer"}""");
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        Assert.True((await connections.TestConnectionAsync(orgA, null, userA, oauth, CancellationToken.None)).Success);

        var apiKey = Base("Key", IntegrationSystemKind.CUSTOM, IntegrationProcessType.CUSTOM, IntegrationProtocol.REST, IntegrationAuthenticationType.API_KEY);
        apiKey.ApiKey = "custom-secret";
        apiKey.Designer = apiKey.Designer with { ApiKeyHeader = "X-API-Key", ApiKeyPlacement = "HEADER", SelectedSourceStructures = [] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        Assert.True((await connections.TestConnectionAsync(orgA, null, userA, apiKey, CancellationToken.None)).Success);
        apiKey.Designer = apiKey.Designer with { ApiKeyPlacement = "QUERY" };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        var queryKey = await connections.TestConnectionAsync(orgA, null, userA, apiKey, CancellationToken.None);
        Assert.True(queryKey.Success);
        Assert.Contains(factory.IntegrationHandler.Requests, uri => uri.Query.Contains("X-API-Key"));

        var bearer = Base("Bearer", IntegrationSystemKind.CUSTOM, IntegrationProcessType.CUSTOM, IntegrationProtocol.REST, IntegrationAuthenticationType.BEARER_TOKEN);
        bearer.BearerToken = "static-secret";
        bearer.Designer = bearer.Designer with { SelectedSourceStructures = [] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        Assert.True((await connections.TestConnectionAsync(orgA, null, userA, bearer, CancellationToken.None)).Success);

        factory.IntegrationHandler.Reset();
        var soap = Base("Ariba SOAP", IntegrationSystemKind.SAP_ARIBA, IntegrationProcessType.GET_PO, IntegrationProtocol.SOAP, IntegrationAuthenticationType.BASIC);
        soap.Username = "soap-user";
        soap.Password = "soap-password";
        soap.Designer = soap.Designer with { WsdlUrl = "https://ariba.example/service?wsdl", SoapOperation = "GetPO", SelectedSourceStructures = ["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "text/xml");
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """<definitions xmlns="http://schemas.xmlsoap.org/wsdl/"><portType><operation name="GetPO"/></portType></definitions>""", "text/xml");
        var soapOk = await connections.TestConnectionAsync(orgA, null, userA, soap, CancellationToken.None);
        Assert.True(soapOk.Success);
        Assert.Contains(soapOk.Checks, item => item.Name == "WSDL" && item.Success);
        Assert.DoesNotContain(factory.IntegrationHandler.Methods, method => method is "POST" or "PUT" or "PATCH" or "DELETE");

        factory.IntegrationHandler.Reset();
        var invoice = Base("Invoice", IntegrationSystemKind.CUSTOM, IntegrationProcessType.POST_INVOICE, IntegrationProtocol.REST, IntegrationAuthenticationType.BEARER_TOKEN);
        invoice.BearerToken = "static-secret";
        invoice.HttpMethod = "POST";
        invoice.ResourcePath = "invoices";
        invoice.Designer = invoice.Designer with { SelectedSourceStructures = ["INVOICE_HEADER", "INVOICE_ITEM"] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        var invoiceTest = await connections.TestConnectionAsync(orgA, null, userA, invoice, CancellationToken.None);
        Assert.True(invoiceTest.Success);
        Assert.DoesNotContain(factory.IntegrationHandler.Requests, uri => uri.AbsolutePath.Contains("invoices") && factory.IntegrationHandler.Methods.Contains("POST"));

        factory.IntegrationHandler.Reset();
        var csrfMissing = IntegrationDesignerCatalog.FiveS4PostGrnSample();
        csrfMissing.Username = "erp-user";
        csrfMissing.Password = "erp-password";
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml");
        var csrfFailed = await connections.TestConnectionAsync(orgA, null, userA, csrfMissing, CancellationToken.None);
        Assert.False(csrfFailed.Success);
        Assert.Equal("CSRF_TOKEN_MISSING", csrfFailed.ErrorCode);
        Assert.Contains("https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/", csrfFailed.RequestJson);

        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.NotFound, "no service");
        var csrfFetchFailed = await connections.TestConnectionAsync(orgA, null, userA, csrfMissing, CancellationToken.None);
        Assert.False(csrfFetchFailed.Success);
        Assert.Equal("CSRF_FETCH_FAILED", csrfFetchFailed.ErrorCode);
        Assert.Contains("https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/", csrfFetchFailed.RequestJson);
        Assert.Contains("\"status\":404", csrfFetchFailed.RequestJson);

        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml", new Dictionary<string, string> { ["X-CSRF-Token"] = "secret-csrf", ["Set-Cookie"] = "SAP_SESSIONID=hidden; Path=/" });
        factory.IntegrationHandler.Enqueue(HttpStatusCode.NotFound, "no metadata");
        var serviceMissing = await connections.TestConnectionAsync(orgA, null, userA, csrfMissing, CancellationToken.None);
        Assert.False(serviceMissing.Success);
        Assert.Equal("SERVICE_NOT_FOUND", serviceMissing.ErrorCode);
        Assert.Contains("https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/$metadata", serviceMissing.RequestJson);

        factory.IntegrationHandler.Reset();
        var deleteDraft = Base("Delete", IntegrationSystemKind.CUSTOM, IntegrationProcessType.CUSTOM, IntegrationProtocol.REST, IntegrationAuthenticationType.NONE);
        deleteDraft.HttpMethod = "DELETE";
        deleteDraft.ResourcePath = "records/1";
        deleteDraft.Designer = deleteDraft.Designer with { SelectedSourceStructures = [] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"ok":true}""");
        var deleteTest = await connections.TestConnectionAsync(orgA, null, userA, deleteDraft, CancellationToken.None);
        Assert.True(deleteTest.Success);
        Assert.DoesNotContain(factory.IntegrationHandler.Methods, method => method == "DELETE");

        var unsafeTarget = Base("Unsafe", IntegrationSystemKind.CUSTOM, IntegrationProcessType.CUSTOM, IntegrationProtocol.REST, IntegrationAuthenticationType.NONE);
        unsafeTarget.BaseUrl = "http://127.0.0.1/secret";
        unsafeTarget.Designer = unsafeTarget.Designer with { SelectedSourceStructures = [] };
        var blocked = await connections.TestConnectionAsync(orgA, null, userA, unsafeTarget, CancellationToken.None);
        Assert.False(blocked.Success);
        Assert.Equal("UNSAFE_TARGET", blocked.ErrorCode);
    }

    [Fact]
    public void S4_csrf_url_is_the_material_document_service_root()
    {
        var sample = IntegrationDesignerCatalog.FiveS4PostGrnSample();
        const string expected = "https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/";
        Assert.Equal(expected, IntegrationODataUrl.CsrfFetchUrl(sample));
        sample.BaseUrl = expected;
        Assert.Equal(expected, IntegrationODataUrl.CsrfFetchUrl(sample));
        sample.Designer = sample.Designer with { CsrfFetchPath = "A_MaterialDocumentHeader" };
        Assert.Equal(expected, IntegrationODataUrl.CsrfFetchUrl(sample));
        sample.Designer = sample.Designer with { CsrfFetchPath = expected };
        Assert.Equal(expected, IntegrationODataUrl.CsrfFetchUrl(sample));
        Assert.Equal("https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/$metadata", IntegrationODataUrl.MetadataUrl(sample.BaseUrl, sample.ServicePath, sample.EntitySet));
        sample.ServicePath = "/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader";
        Assert.Equal("https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/$metadata", IntegrationODataUrl.MetadataUrl(sample.BaseUrl, sample.ServicePath, sample.EntitySet));
    }

    private void EnqueueS4Success()
    {
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml", new Dictionary<string, string> { ["X-CSRF-Token"] = "secret-csrf", ["Set-Cookie"] = "SAP_SESSIONID=hidden; Path=/" });
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """
            <edmx:Edmx xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx" Version="1.0">
              <edmx:DataServices>
                <Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm" Namespace="API_MATERIAL_DOCUMENT_SRV">
                  <EntityType Name="A_MaterialDocumentHeaderType"><Key><PropertyRef Name="MaterialDocument"/></Key><Property Name="MaterialDocument" Type="Edm.String"/></EntityType>
                  <EntityContainer Name="API_MATERIAL_DOCUMENT_SRV_Entities"><EntitySet Name="A_MaterialDocumentHeader" EntityType="API_MATERIAL_DOCUMENT_SRV.A_MaterialDocumentHeaderType"/></EntityContainer>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """, "application/xml");
    }

    private static IntegrationDesignerDraft Base(string name, IntegrationSystemKind system, IntegrationProcessType process, IntegrationProtocol protocol, IntegrationAuthenticationType auth) => new()
    {
        Name = name, SystemKind = system, ProcessType = process, Protocol = protocol, AuthenticationType = auth,
        BaseUrl = "https://api.example.test", HttpMethod = process.ToString().StartsWith("POST") ? "POST" : "GET",
        Designer = IntegrationDesignerCatalog.DefaultsFor(system, process, protocol),
    };

    private static async Task<Guid> CreateOrgAsync(SilaMeDbContext db, string prefix)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"{prefix}-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = prefix, Kind = OrganizationKind.CUSTOMER, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();
        return organization.Id;
    }
}
