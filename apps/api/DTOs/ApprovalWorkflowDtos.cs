namespace SilaMe.Api.DTOs;

public sealed record ApprovalWorkflowLevelInput(int Level, string RoleKey, string Label);
public sealed record ApprovalWorkflowConditionInput(string FieldKey, string Operator, decimal Value);

public sealed record ApprovalWorkflowConfigurationRow(
    Guid Id,
    string Name,
    string ApprovalType,
    string Action,
    string ScopeKind,
    string? ScopeValue,
    bool IsActive,
    IReadOnlyList<ApprovalWorkflowConditionInput> Conditions,
    IReadOnlyList<ApprovalWorkflowLevelInput> Levels,
    DateTime UpdatedAt);

public sealed record ApprovalWorkflowUpsertRequest(
    string Name,
    string ApprovalType,
    string Action,
    string ScopeKind,
    string? ScopeValue,
    bool IsActive,
    IReadOnlyList<ApprovalWorkflowConditionInput> Conditions,
    IReadOnlyList<ApprovalWorkflowLevelInput> Levels);

public sealed record ApprovalFieldCatalogItem(string Key, string Label, string Unit);
public sealed record ApprovalTypeCatalogItem(string Key, string Label, string RuntimeNote, IReadOnlyList<ApprovalFieldCatalogItem> Fields);
public sealed record ApprovalRoleOption(string Key, string Name);
public sealed record ApprovalScopeOption(string Value, string Label);

public sealed record ApprovalWorkflowCatalog(
    IReadOnlyList<ApprovalTypeCatalogItem> ApprovalTypes,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> ScopeKinds,
    IReadOnlyList<string> Operators,
    IReadOnlyList<ApprovalRoleOption> Roles);
