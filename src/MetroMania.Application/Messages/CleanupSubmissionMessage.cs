namespace MetroMania.Application.Messages;

public record CleanupSubmissionMessage(Guid SubmissionId, List<CleanupSubmissionMessage.RenderInfo> Renders)
{
    public record RenderInfo(Guid LevelId, int TotalFrames);
}
