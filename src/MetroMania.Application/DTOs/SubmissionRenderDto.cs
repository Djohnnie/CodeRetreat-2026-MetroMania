namespace MetroMania.Application.DTOs;

public record SubmissionRenderDto(
    Guid Id,
    Guid SubmissionId,
    Guid LevelId,
    int Day,
    string SvgContent);
