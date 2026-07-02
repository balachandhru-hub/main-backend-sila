using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Identity.Domain.Entities
{
    public class RoleFeatureMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public Guid FeatureId { get; set; }

        public Guid RoleId { get; set; }


        public RoleFeatureMapping()
        {}
    }
}