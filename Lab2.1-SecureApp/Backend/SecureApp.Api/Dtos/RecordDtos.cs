using System.ComponentModel.DataAnnotations;

namespace SecureApp.Api.Dtos;

public record RecordRequest(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(10_000)] string Content);

public record RecordResponse(int Id, string Title, string Content, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
