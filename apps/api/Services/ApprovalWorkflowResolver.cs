using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed record ApprovalWorkflowMatchContext(
    ApprovalWorkflowType ApprovalType,
    ApprovalWorkflowAction Action,
    string? PropertyCode,
    string? CompanyCode,
    string? OutletCode,
    string? StoreCode,
    IReadOnlyDictionary<string, decimal?> Fields);

public static class ApprovalWorkflowResolver
{
    public static ApprovalWorkflowConfiguration? Resolve(
        IEnumerable<ApprovalWorkflowConfiguration> configurations,
        ApprovalWorkflowMatchContext context)
    {
        var ranked = configurations
            .Where(item => item.IsActive && item.ApprovalType == context.ApprovalType)
            .Where(item => item.Action == ApprovalWorkflowAction.ALL || item.Action == context.Action)
            .Where(item => ScopeMatches(item, context))
            .Where(item => ConditionsMatch(item, context.Fields))
            .Select(item => new
            {
                Item = item,
                Specificity = ScopeRank(item.ScopeKind),
                ActionRank = item.Action == ApprovalWorkflowAction.ALL ? 0 : 1,
                ConditionCount = item.Conditions.Count,
            })
            .OrderByDescending(item => item.Specificity)
            .ThenByDescending(item => item.ActionRank)
            .ThenByDescending(item => item.ConditionCount)
            .ToList();
        return ranked.FirstOrDefault()?.Item;
    }

    public static IReadOnlyList<(int Level, string RoleKey, string Label)> Levels(ApprovalWorkflowConfiguration configuration) =>
        configuration.Levels.OrderBy(item => item.Level).Select(item => (item.Level, item.RoleKey, item.Label)).ToList();

    public static bool ConditionsMatch(ApprovalWorkflowConfiguration configuration, IReadOnlyDictionary<string, decimal?> fields)
    {
        foreach (var condition in configuration.Conditions)
        {
            if (!fields.TryGetValue(condition.FieldKey.ToUpperInvariant(), out var actual) || actual is null)
                return false;
            var passes = condition.Operator == ApprovalConditionOperator.GREATER_THAN
                ? actual.Value > condition.Value
                : actual.Value < condition.Value;
            if (!passes) return false;
        }
        return true;
    }

    public static bool ScopeMatches(ApprovalWorkflowConfiguration configuration, ApprovalWorkflowMatchContext context)
    {
        if (configuration.ScopeKind == ApprovalScopeKind.ALL) return true;
        var expected = (configuration.ScopeValue ?? string.Empty).Trim().ToUpperInvariant();
        if (expected.Length == 0) return false;
        var actual = configuration.ScopeKind switch
        {
            ApprovalScopeKind.PROPERTY => context.PropertyCode,
            ApprovalScopeKind.COMPANY_CODE => context.CompanyCode,
            ApprovalScopeKind.OUTLET => context.OutletCode,
            ApprovalScopeKind.STORE => context.StoreCode,
            _ => null,
        };
        return string.Equals(actual?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    public static int ScopeRank(ApprovalScopeKind kind) => kind switch
    {
        ApprovalScopeKind.STORE => 4,
        ApprovalScopeKind.OUTLET => 3,
        ApprovalScopeKind.PROPERTY => 2,
        ApprovalScopeKind.COMPANY_CODE => 1,
        _ => 0,
    };
}
