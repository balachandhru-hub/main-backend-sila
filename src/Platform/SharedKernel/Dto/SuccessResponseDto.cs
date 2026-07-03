using System.Runtime.Serialization;

namespace Dto;

/// <summary>
/// Success response model.
/// </summary>
[DataContract]
public class SuccessResponseDto : ErrorResponseDto
{
    /// <summary>
    /// Created/Updated resource Id.
    /// </summary>
    [DataMember(Name = "id")]
    public string? Id { get; set; }
}