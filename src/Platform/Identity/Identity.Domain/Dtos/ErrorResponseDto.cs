using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
 
namespace Identity.Domain.Dtos
{
    /// <summary>
    /// ErrorResponse model used to bind the error message for API response.
    /// </summary>
    [DataContract]
    public class ErrorResponseDto
    {
        /// <summary>
        /// An error response status code for an operation.
        /// </summary>
        /// <value>An error response status code for an operation.</value>
 
        [DataMember(Name = "status_code")]
        public int? StatusCode { get; set; }
 
        /// <summary>
        /// An error response message for an operation.
        /// </summary>
        /// <value>An error response message for an operation.</value>
 
        [DataMember(Name = "message")]
        [Required]
        public string? Message { get; set; }
 
        /// <summary>
        /// The detailed error response for an operation.
        /// </summary>
        /// <value>The detailed error response for an operation.</value>
 
        [DataMember(Name = "description")]
        [Required]
        public string? Description { get; set; }
 
    }
}
 