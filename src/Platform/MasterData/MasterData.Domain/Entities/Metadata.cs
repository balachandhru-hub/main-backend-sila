using System.ComponentModel.DataAnnotations;
using MasterData.Domain.Common;

namespace MasterData.Domain.Entities;

public class Metadata : BaseEntity
{
    [Key]
    [Required]
    public Guid Id { get; set; }

    public string Key { get; set; }

    public string Type { get; set; }

    public string? Description { get; set; }
}