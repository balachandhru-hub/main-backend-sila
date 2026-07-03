using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Identity.Domain.Entities
{
    public class Feature : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public string Key { get; set; }


        public Feature()
        {}
    }
}