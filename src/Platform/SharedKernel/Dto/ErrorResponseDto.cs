using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Dto;

/// <summary>
/// ErrorResponse model used to bind the error message for API response.
/// </summary>
[DataContract]
public class ErrorResponseDto
{
    /// <summary>
    /// Error status code.
    /// </summary>
    [DataMember(Name = "status_code")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    [DataMember(Name = "message")]
    [Required]
    public string? Message { get; set; }

    /// <summary>
    /// Detailed error description.
    /// </summary>
    [DataMember(Name = "description")]
    [Required]
    public string? Description { get; set; }
}