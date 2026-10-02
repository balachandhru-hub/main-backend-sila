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

public sealed class IntegrationDesignerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void System_interface_and_auth_matrix_hides_irrelevant_protocols()
    {
        Assert.Equal([IntegrationProtocol.ODATA_V2, IntegrationProtocol.ODATA_V4, IntegrationProtocol.REST], IntegrationDesignerCatalog.InterfacesBySystem[IntegrationSystemKind.SAP_S4HANA]);
        Assert.Equal([IntegrationProtocol.REST, IntegrationProtocol.SOAP], IntegrationDesignerCatalog.InterfacesBySystem[IntegrationSystemKind.SAP_ARIBA]);
        Assert.Equal([IntegrationProtocol.REST, IntegrationProtocol.SOAP], IntegrationDesignerCatalog.InterfacesBySystem[IntegrationSystemKind.ORACLE]);
        Assert.Equal([IntegrationProtocol.REST], IntegrationDesignerCatalog.InterfacesBySystem[IntegrationSystemKind.ODOO]);
        Assert.Contains(IntegrationProtocol.ODATA_V4, IntegrationDesignerCatalog.InterfacesBySystem[IntegrationSystemKind.CUSTOM]);
        Assert.DoesNotContain(IntegrationProtocol.SOAP, IntegrationDesignerCatalog.InterfacesBySystem[IntegrationSystemKind.SAP_S4HANA]);
        Assert.DoesNotContain(IntegrationProtocol.ODATA_V2, IntegrationDesignerCatalog.InterfacesBySystem[IntegrationSystemKind.SAP_ARIBA]);
        Assert.Equal([IntegrationAuthenticationType.BASIC, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS], IntegrationDesignerCatalog.AuthenticationFor(IntegrationSystemKind.SAP_S4HANA, IntegrationProtocol.ODATA_V2));
        Assert.Equal([IntegrationAuthenticationType.BASIC], IntegrationDesignerCatalog.AuthenticationFor(IntegrationSystemKind.SAP_ARIBA, IntegrationProtocol.SOAP));
        Assert.Contains(IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT, IntegrationDesignerCatalog.AuthenticationFor(IntegrationSystemKind.SAP_ARIBA, IntegrationProtocol.REST));
        Assert.Contains(IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS, IntegrationDesignerCatalog.AuthenticationFor(IntegrationSystemKind.ORACLE, IntegrationProtocol.REST));
        Assert.Contains(IntegrationAuthenticationType.API_KEY, IntegrationDesignerCatalog.AuthenticationFor(IntegrationSystemKind.CUSTOM, IntegrationProtocol.REST));
        Assert.Equal(IntegrationMappingDirection.SILA_TO_EXTERNAL, IntegrationDesignerCatalog.Direction(IntegrationProcessType.POST_GRN));
        Assert.Equal(IntegrationMappingDirection.EXTERNAL_TO_SILA, IntegrationDesignerCatalog.Direction(IntegrationProcessType.GET_PO));
    }

    [Fact]
    public void Source_registry_and_recommendations_match_canonical_models()
    {
        var structures = IntegrationDesignerCatalog.SourceStructures().Select(item => item.Code).ToHashSet();
        foreach (var expected in new[] { "PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM", "INVOICE_HEADER", "INVOICE_ITEM", "INVOICE_EXTRACTION", "GRN_HEADER", "GRN_ITEM", "SUPPLIER", "MATERIAL", "INVENTORY", "COMPANY_CODE", "PLANT", "STORAGE_LOCATION", "DOCUMENT", "USER_CONTEXT" })
            Assert.Contains(expected, structures);
        var grn = IntegrationDesignerCatalog.Recommendations()[IntegrationProcessType.POST_GRN];
        Assert.Equal(["GRN_HEADER", "GRN_ITEM"], grn.Primary);
        Assert.Equal(["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"], grn.Recommended);
        var invoice = IntegrationDesignerCatalog.Recommendations()[IntegrationProcessType.POST_INVOICE];
        Assert.Contains("INVOICE_HEADER", invoice.Primary);
        Assert.Contains("INVOICE_EXTRACTION", invoice.Recommended);
        Assert.Contains("GRN_HEADER", invoice.Recommended);
        var po = IntegrationDesignerCatalog.Recommendations()[IntegrationProcessType.POST_PO];
        Assert.Equal(["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"], po.Primary);
        Assert.Contains(IntegrationDesignerCatalog.SourceStructures().Single(item => item.Code == "GRN_HEADER").Fields, field => field.Name == "ReceiptDate");
        Assert.Contains(IntegrationDesignerCatalog.SourceStructures().Single(item => item.Code == "PURCHASE_ORDER_ITEM").Fields, field => field.Name == "OpenQuantity");
    }

    [Fact]
    public async Task Designer_tests_s4_ariba_oracle_custom_and_enforces_validation_fingerprint()
    {
        factory.IntegrationHandler.Reset();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var designer = scope.ServiceProvider.GetRequiredService<IntegrationDesignerService>();
        var prodOrg = await CreateOrgAsync(db, "FIVE-PROD");
        var testOrg = await CreateOrgAsync(db, "FIVE-TEST");

        var s4 = IntegrationDesignerCatalog.FiveS4PostGrnSample();
        s4.Username = "erp-user";
        s4.Password = "erp-password";
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<edmx/>", "application/xml");
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml", new Dictionary<string, string> { ["X-CSRF-Token"] = "secret-csrf", ["Set-Cookie"] = "SAP_SESSIONID=hidden; Path=/" });
        var auth = await designer.TestDraftAsync(testOrg, s4, "AUTH", null, CancellationToken.None);
        Assert.True(auth.Success);
        Assert.True(auth.CsrfAcquired);
        Assert.DoesNotContain("erp-password", auth.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-csrf", auth.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(auth.Fingerprint);
        s4.BaseUrl = "https://changed.example";
        s4.ValidationFingerprint = auth.Fingerprint;
        var saveBlocked = await Assert.ThrowsAsync<IntegrationException>(() => designer.SaveValidatedAsync(testOrg, null, s4, "tester", CancellationToken.None));
        Assert.Equal("SAVE_REQUIRES_TEST", saveBlocked.Code);
        var draftSaved = await designer.SaveDraftAsync(testOrg, null, s4, CancellationToken.None);
        Assert.Equal(IntegrationConfigurationStatus.DRAFT, draftSaved.Status);
        Assert.Equal("NOT_TESTED", draftSaved.ValidationStatus);
        s4.BaseUrl = "https://my419951-api.s4hana.cloud.sap";

        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """
            <edmx:Edmx xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx" Version="1.0">
              <edmx:DataServices>
                <Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm" Namespace="API_MATERIAL_DOCUMENT_SRV">
                  <EntityType Name="A_MaterialDocumentHeaderType">
                    <Key><PropertyRef Name="MaterialDocument"/></Key>
                    <Property Name="MaterialDocument" Type="Edm.String" Nullable="false"/>
                    <Property Name="MaterialDocumentYear" Type="Edm.String" Nullable="false"/>
                    <NavigationProperty Name="to_MaterialDocumentItem"/>
                  </EntityType>
                  <EntityContainer Name="API_MATERIAL_DOCUMENT_SRV_Entities">
                    <EntitySet Name="A_MaterialDocumentHeader" EntityType="API_MATERIAL_DOCUMENT_SRV.A_MaterialDocumentHeaderType"/>
                  </EntityContainer>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """, "application/xml");
        var schema = await designer.TestDraftAsync(testOrg, s4, "SCHEMA", null, CancellationToken.None);
        Assert.True(schema.Success);
        Assert.Contains(schema.Entities!, entity => entity.EntitySet == "A_MaterialDocumentHeader" || entity.Name.Contains("MaterialDocumentHeader"));

        var mapping = await designer.TestDraftAsync(testOrg, s4, "MAPPING", null, CancellationToken.None);
        Assert.True(mapping.Success);
        Assert.Contains(s4.Mappings, item => item.IsCollection || item.TargetField.Contains("MaterialDocumentItems[]"));

        var preview = designer.Preview(s4);
        Assert.True(preview.PreviewOnly);
        Assert.Contains("NOTHING HAS BEEN POSTED", preview.Notice);
        Assert.Equal("POST", preview.Method);
        Assert.Contains("A_MaterialDocumentHeader", preview.Url);
        Assert.Contains("••••", preview.Headers["Authorization"]);
        Assert.DoesNotContain("erp-password", JsonSerializer.Serialize(preview));

        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml", new Dictionary<string, string> { ["X-CSRF-Token"] = "secret-csrf", ["Set-Cookie"] = "SAP_SESSIONID=hidden; Path=/" });
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """
            <edmx:Edmx xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx" Version="1.0">
              <edmx:DataServices>
                <Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm" Namespace="API_MATERIAL_DOCUMENT_SRV">
                  <EntityType Name="A_MaterialDocumentHeaderType"><Key><PropertyRef Name="MaterialDocument"/></Key><Property Name="MaterialDocument" Type="Edm.String" Nullable="false"/></EntityType>
                  <EntityContainer Name="API_MATERIAL_DOCUMENT_SRV_Entities"><EntitySet Name="A_MaterialDocumentHeader" EntityType="API_MATERIAL_DOCUMENT_SRV.A_MaterialDocumentHeaderType"/></EntityContainer>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """, "application/xml");
        var connections = scope.ServiceProvider.GetRequiredService<IntegrationConnectionTestService>();
        var retest = await connections.TestConnectionAsync(testOrg, null, Guid.NewGuid(), s4, CancellationToken.None);
        s4.TestId = retest.TestId;
        await connections.EnsureValidProofAsync(testOrg, null, s4, null, CancellationToken.None);
        var saved = await designer.SaveValidatedAsync(testOrg, null, s4, "tester", CancellationToken.None);
        Assert.Equal(IntegrationConfigurationStatus.VALIDATED, saved.Status);
        Assert.Equal("Configured", saved.CredentialStatus);
        Assert.Null(saved.Designer?.Headers?.FirstOrDefault(item => item.IsSecret)?.Value);
        var stored = await db.ApiIntegrationConfigurations.SingleAsync(item => item.Id == saved.Id);
        Assert.DoesNotContain("erp-password", stored.ProtectedPassword);
        Assert.Equal("TEST", stored.EnvironmentCode);

        s4.BaseUrl = "https://changed.example";
        Assert.NotEqual(saved.ValidationFingerprint, designer.Fingerprint(s4, stored));
        s4.BaseUrl = "https://my419951-api.s4hana.cloud.sap";
        s4.Password = "changed-secret";
        Assert.NotEqual(saved.ValidationFingerprint, designer.Fingerprint(s4, stored));
        s4.Password = "erp-password";
        s4.Mappings.Add(new DesignerMappingInput { SourceKind = IntegrationMappingSourceKind.CONSTANT, SourceField = "X", TargetField = "GoodsMovementCode", DefaultValue = "501" });
        Assert.NotEqual(saved.ValidationFingerprint, designer.Fingerprint(s4, stored));
        s4.Mappings.RemoveAt(s4.Mappings.Count - 1);

        await designer.ActivateAsync(testOrg, saved.Id, true, CancellationToken.None);
        Assert.Equal(IntegrationConfigurationStatus.ACTIVE, (await db.ApiIntegrationConfigurations.AsNoTracking().SingleAsync(item => item.Id == saved.Id)).Status);

        var draftActivate = IntegrationDesignerCatalog.FiveS4PostGrnSample();
        draftActivate.Username = "u";
        draftActivate.Password = "p";
        factory.IntegrationHandler.Enqueue(HttpStatusCode.Unauthorized, "no");
        var failed = await designer.TestDraftAsync(testOrg, draftActivate, "AUTH", null, CancellationToken.None);
        Assert.False(failed.Success);
        draftActivate.ValidationFingerprint = failed.Fingerprint;
        await Assert.ThrowsAsync<IntegrationException>(() => designer.SaveValidatedAsync(testOrg, null, draftActivate, "tester", CancellationToken.None));

        var aribaSoap = BaseDraft("Ariba SOAP", IntegrationSystemKind.SAP_ARIBA, IntegrationProcessType.GET_PO, IntegrationProtocol.SOAP, IntegrationAuthenticationType.BASIC);
        aribaSoap.Designer = aribaSoap.Designer with { WsdlUrl = "https://ariba.example/service?wsdl", SoapOperation = "GetPO", SelectedSourceStructures = ["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """<definitions xmlns="http://schemas.xmlsoap.org/wsdl/"><portType><operation name="GetPO"/></portType></definitions>""", "text/xml");
        Assert.True((await designer.TestDraftAsync(testOrg, aribaSoap, "ENDPOINT", null, CancellationToken.None)).Success);

        var aribaRest = BaseDraft("Ariba REST", IntegrationSystemKind.SAP_ARIBA, IntegrationProcessType.GET_PO, IntegrationProtocol.REST, IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT);
        aribaRest.TokenEndpoint = "https://api.ariba.com/v2/oauth/token";
        aribaRest.TokenBody = new Dictionary<string, string> { ["grant_type"] = "openapi_2lo" };
        aribaRest.Designer = aribaRest.Designer with { TokenResponsePath = "access_token", SelectedSourceStructures = ["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"access_token":"secret-token","token_type":"Bearer"}""");
        var tokenTest = await designer.TestDraftAsync(testOrg, aribaRest, "AUTH", null, CancellationToken.None);
        Assert.True(tokenTest.Success);
        Assert.True(tokenTest.TokenAcquired);
        Assert.DoesNotContain("secret-token", tokenTest.Message);

        var oracle = BaseDraft("Oracle REST", IntegrationSystemKind.ORACLE, IntegrationProcessType.GET_SUPPLIER, IntegrationProtocol.REST, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS);
        oracle.TokenEndpoint = "https://oracle.example/oauth/token";
        oracle.ClientId = "id";
        oracle.ClientSecret = "oracle-secret";
        oracle.Designer = oracle.Designer with { SelectedSourceStructures = ["SUPPLIER"] };
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"access_token":"oracle-token"}""");
        Assert.True((await designer.TestDraftAsync(testOrg, oracle, "AUTH", null, CancellationToken.None)).Success);

        var custom = BaseDraft("Custom REST", IntegrationSystemKind.CUSTOM, IntegrationProcessType.CUSTOM, IntegrationProtocol.REST, IntegrationAuthenticationType.API_KEY);
        custom.ApiKey = "custom-secret";
        custom.Designer = custom.Designer with { ApiKeyHeader = "X-API-Key", SamplePayload = """{"id":"1","items":[{"qty":1}]}""", SelectedSourceStructures = [] };
        var customSchema = await designer.TestDraftAsync(testOrg, custom, "SCHEMA", null, CancellationToken.None);
        Assert.True(customSchema.Success);
        Assert.Contains(customSchema.Entities!, entity => entity.Properties.Any(property => property.Name.Contains("items")));

        var prodDraft = IntegrationDesignerCatalog.FiveS4PostGrnSample();
        prodDraft.Name = "PROD leak check";
        prodDraft.Username = "u";
        prodDraft.Password = "p";
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml", new Dictionary<string, string> { ["X-CSRF-Token"] = "t", ["Set-Cookie"] = "SAP_SESSIONID=hidden; Path=/" });
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
        var prodTest = await connections.TestConnectionAsync(prodOrg, null, Guid.NewGuid(), prodDraft, CancellationToken.None);
        prodDraft.TestId = prodTest.TestId;
        await connections.EnsureValidProofAsync(prodOrg, null, prodDraft, null, CancellationToken.None);
        var prodSaved = await designer.SaveValidatedAsync(prodOrg, null, prodDraft, "tester", CancellationToken.None);
        db.ApiIntegrationConfigurations.Single(item => item.Id == prodSaved.Id).EnvironmentCode = "PROD";
        await db.SaveChangesAsync();
        var testList = await scope.ServiceProvider.GetRequiredService<IntegrationService>().ListAsync(testOrg, CancellationToken.None, "TEST");
        Assert.DoesNotContain(testList, item => item.Id == prodSaved.Id);
        Assert.DoesNotContain(testList, item => item.OrganizationId == prodOrg);
        var prodList = await scope.ServiceProvider.GetRequiredService<IntegrationService>().ListAsync(prodOrg, CancellationToken.None, "TEST");
        Assert.DoesNotContain(prodList, item => item.EnvironmentCode == "PROD");
    }

    [Fact]
    public void Odata_and_sample_parsers_load_destination_schema()
    {
        var odata = IntegrationDesignerSchemaParser.ParseOData("""
            <edmx:Edmx xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx"><edmx:DataServices>
            <Schema xmlns="http://docs.oasis-open.org/odata/ns/edm"><EntityType Name="Header"><Property Name="PostingDate" Type="Edm.Date"/></EntityType>
            <EntityContainer><EntitySet Name="A_MaterialDocumentHeader" EntityType="Header"/></EntityContainer></Schema></edmx:DataServices></edmx:Edmx>
            """);
        Assert.Contains(odata, item => item.EntitySet == "A_MaterialDocumentHeader");
        var rest = IntegrationDesignerSchemaParser.ParseJsonSample("""{"PostingDate":"2026-09-19","MaterialDocumentItems":[{"Material":"M"}]}""");
        Assert.Contains(rest[0].Properties, item => item.Name.Contains("MaterialDocumentItems"));
        var wsdl = IntegrationDesignerSchemaParser.ParseWsdl("""<definitions xmlns="http://schemas.xmlsoap.org/wsdl/"><portType><operation name="SubmitRequisition"/></portType></definitions>""");
        Assert.Contains(wsdl, item => item.Name == "SubmitRequisition");
    }

    private static IntegrationDesignerDraft BaseDraft(string name, IntegrationSystemKind system, IntegrationProcessType process, IntegrationProtocol protocol, IntegrationAuthenticationType auth) => new()
    {
        Name = name, SystemKind = system, ProcessType = process, Protocol = protocol, AuthenticationType = auth,
        BaseUrl = "https://api.example.test", HttpMethod = process.ToString().StartsWith("POST") ? "POST" : "GET",
        Designer = IntegrationDesignerCatalog.DefaultsFor(system, process, protocol),
        Mappings = [new DesignerMappingInput { SourceKind = IntegrationMappingSourceKind.FIELD, SourceStructure = IntegrationDesignerCatalog.IsPush(process) ? "GRN_HEADER" : "PURCHASE_ORDER_HEADER", SourceField = "PoNumber", TargetField = "PurchaseOrder" }]
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
