using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class ApprovalWorkflowResolverTests
{
    [Fact]
    public void Store_scope_beats_company_code_and_all()
    {
        var all = Config("all", ApprovalScopeKind.ALL, null);
        var company = Config("cc", ApprovalScopeKind.COMPANY_CODE, "1000");
        var store = Config("store", ApprovalScopeKind.STORE, "MAIN");
        var match = ApprovalWorkflowResolver.Resolve([all, company, store], Context(companyCode: "1000", storeCode: "MAIN"));
        Assert.Equal(store.Id, match?.Id);
    }

    [Fact]
    public void Greater_than_condition_filters_out_low_values()
    {
        var high = Config("high", ApprovalScopeKind.ALL, null, ("GROSS_AMOUNT", ApprovalConditionOperator.GREATER_THAN, 1000m));
        var context = Context(fields: new Dictionary<string, decimal?> { ["GROSS_AMOUNT"] = 200m });
        Assert.Null(ApprovalWorkflowResolver.Resolve([high], context with { ApprovalType = ApprovalWorkflowType.INVOICE }));
        var invoiceHigh = Config("invoice", ApprovalScopeKind.ALL, null, ("GROSS_AMOUNT", ApprovalConditionOperator.GREATER_THAN, 1000m));
        invoiceHigh.ApprovalType = ApprovalWorkflowType.INVOICE;
        var pass = Context(fields: new Dictionary<string, decimal?> { ["GROSS_AMOUNT"] = 1500m }) with { ApprovalType = ApprovalWorkflowType.INVOICE };
        Assert.Equal(invoiceHigh.Id, ApprovalWorkflowResolver.Resolve([invoiceHigh], pass)?.Id);
    }

    [Fact]
    public void Create_action_beats_all_when_both_match()
    {
        var generic = Config("all-action", ApprovalScopeKind.ALL, null);
        generic.Action = ApprovalWorkflowAction.ALL;
        var create = Config("create", ApprovalScopeKind.ALL, null);
        create.Action = ApprovalWorkflowAction.CREATE;
        var match = ApprovalWorkflowResolver.Resolve([generic, create], Context());
        Assert.Equal(create.Id, match?.Id);
    }

    private static ApprovalWorkflowMatchContext Context(
        string? companyCode = null,
        string? storeCode = null,
        IReadOnlyDictionary<string, decimal?>? fields = null) =>
        new(ApprovalWorkflowType.RECIPE, ApprovalWorkflowAction.CREATE, null, companyCode, null, storeCode,
            fields ?? new Dictionary<string, decimal?>());

    private static ApprovalWorkflowConfiguration Config(
        string name,
        ApprovalScopeKind scope,
        string? value,
        params (string Field, ApprovalConditionOperator Operator, decimal Amount)[] conditions)
    {
        var row = new ApprovalWorkflowConfiguration
        {
            Id = Guid.NewGuid(), Name = name, ApprovalType = ApprovalWorkflowType.RECIPE,
            Action = ApprovalWorkflowAction.ALL, ScopeKind = scope, ScopeValue = value, IsActive = true,
        };
        foreach (var condition in conditions)
        {
            row.Conditions.Add(new ApprovalWorkflowCondition
            {
                Id = Guid.NewGuid(), FieldKey = condition.Field, Operator = condition.Operator, Value = condition.Amount,
            });
        }
        row.Levels.Add(new ApprovalWorkflowLevel { Id = Guid.NewGuid(), Level = 1, RoleKey = "APPROVER", Label = "Approver" });
        return row;
    }
}
