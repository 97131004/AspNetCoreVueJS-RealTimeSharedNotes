using AspNetCore_RealTimeSharedNotes.Models.ViewModels;

namespace AspNetCore_RealTimeSharedNotes.Models.Responses;

public record CreateUserResponse(bool Success, string? Error, ApiKeyViewModel? ApiKey);
