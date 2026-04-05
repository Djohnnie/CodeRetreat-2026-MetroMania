using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Interfaces;
using MetroMania.Domain.Enums;

namespace MetroMania.Application.Levels.Commands;

public record CreateLevelCommand(
    string Title,
    string Description,
    int GridWidth,
    int GridHeight,
    Dictionary<string, LocalizedLevelText> LocalizedContent) : IRequest<LevelDto>;

public class CreateLevelCommandHandler(ILevelRepository levelRepository)
    : IRequestHandler<CreateLevelCommand, LevelDto>
{
    public async Task<LevelDto> Handle(CreateLevelCommand request, CancellationToken cancellationToken)
    {
        var maxOrder = await levelRepository.GetMaxSortOrderAsync();

        // Use the English localized title as the canonical Title when available
        var canonicalTitle = request.LocalizedContent.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en.Title)
            ? en.Title
            : request.Title;
        var canonicalDescription = request.LocalizedContent.TryGetValue("en", out var enDesc) && !string.IsNullOrEmpty(enDesc.Description)
            ? enDesc.Description
            : request.Description;

        var level = new Level
        {
            Id = Guid.NewGuid(),
            Title = canonicalTitle,
            Description = canonicalDescription,
            GridWidth = request.GridWidth,
            GridHeight = request.GridHeight,
            SortOrder = maxOrder + 1,
            LevelData = new LevelData
            {
                Seed = Random.Shared.Next(),
                LocalizedContent = request.LocalizedContent,
                InitialResources = [ResourceType.Line, ResourceType.Train]
            }
        };

        await levelRepository.AddAsync(level);
        return LevelDto.FromEntity(level);
    }
}
