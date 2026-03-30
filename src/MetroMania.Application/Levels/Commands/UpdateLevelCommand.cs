using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Levels.Commands;

public record UpdateLevelCommand(
    Guid Id,
    string Title,
    string Description,
    int GridWidth,
    int GridHeight,
    Dictionary<string, LocalizedLevelText> LocalizedContent) : IRequest<LevelDto?>;

public class UpdateLevelCommandHandler(ILevelRepository levelRepository)
    : IRequestHandler<UpdateLevelCommand, LevelDto?>
{
    public async Task<LevelDto?> Handle(UpdateLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await levelRepository.GetByIdAsync(request.Id);
        if (level is null) return null;

        // Use the English localized title as the canonical Title when available
        level.Title = request.LocalizedContent.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en.Title)
            ? en.Title
            : request.Title;
        level.Description = request.LocalizedContent.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc.Description)
            ? enDesc.Description
            : request.Description;
        level.GridWidth = request.GridWidth;
        level.GridHeight = request.GridHeight;
        level.LevelData.LocalizedContent = request.LocalizedContent;
        await levelRepository.UpdateAsync(level);

        return LevelDto.FromEntity(level);
    }
}
