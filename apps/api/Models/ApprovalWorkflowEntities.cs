namespace SilaMe.Api.Models;

public enum ApprovalWorkflowType { RECIPE, MATERIAL_MASTER, SUPPLIER_MASTER, INTERNAL_TRANSFER, GRN, INVOICE }
public enum ApprovalWorkflowAction { ALL, CREATE, CHANGE }
public enum ApprovalScopeKind { ALL, PROPERTY, COMPANY_CODE, OUTLET, STORE }
public enum ApprovalConditionOperator { GREATER_THAN, LESS_THAN }

public sealed class ApprovalWorkflowConfiguration
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public ApprovalWorkflowType ApprovalType { get; set; }
    public ApprovalWorkflowAction Action { get; set; } = ApprovalWorkflowAction.ALL;
    public ApprovalScopeKind ScopeKind { get; set; } = ApprovalScopeKind.ALL;
    public string? ScopeValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<ApprovalWorkflowLevel> Levels { get; set; } = [];
    public ICollection<ApprovalWorkflowCondition> Conditions { get; set; } = [];
}

public sealed class ApprovalWorkflowLevel
{
    public Guid Id { get; set; }
    public Guid ConfigurationId { get; set; }
    public int Level { get; set; }
    public required string RoleKey { get; set; }
    public required string Label { get; set; }
    public ApprovalWorkflowConfiguration Configuration { get; set; } = null!;
}

public sealed class ApprovalWorkflowCondition
{
    public Guid Id { get; set; }
    public Guid ConfigurationId { get; set; }
    public required string FieldKey { get; set; }
    public ApprovalConditionOperator Operator { get; set; }
    public decimal Value { get; set; }
    public ApprovalWorkflowConfiguration Configuration { get; set; } = null!;
}
