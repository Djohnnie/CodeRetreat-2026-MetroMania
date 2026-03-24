using MetroMania.Domain.Enums;

namespace MetroMania.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    UserRole Role,
    ApprovalStatus ApprovalStatus,
    bool IsDarkMode,
    string Language,
    DateTime CreatedAt);
