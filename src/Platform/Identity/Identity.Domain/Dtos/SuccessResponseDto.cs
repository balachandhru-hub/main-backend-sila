using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Identity.Domain.Dtos
{
    /// <summary>
    /// SuccessResponse model used to bind the success message for API response.
    /// </summary>
    [DataContract]
    public partial class SuccessResponseDto : ErrorResponseDto
    {
        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        [DataMember(Name = "id")]
        public String? Id { get; set; }


    }
}
