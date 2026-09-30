
using SharedKernel.Models;
namespace Identity.Domain.Entities
{
    public class ModelMapping : BaseModel
    {
        public Guid Id { get; set; }

        public string Key { get; set; }

        public string ModelName { get; set; }
        public ModelMapping()
        {
        }

    }
}