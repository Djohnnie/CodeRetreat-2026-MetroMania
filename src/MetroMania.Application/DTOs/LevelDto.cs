namespace MetroMania.Application.DTOs;

public record LevelDto(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    DateTime CreatedAt);
