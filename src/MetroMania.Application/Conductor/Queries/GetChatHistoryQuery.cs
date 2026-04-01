using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Conductor.Queries;

public record GetChatHistoryQuery(Guid UserId) : IRequest<List<ChatMessageDto>>;

public class GetChatHistoryQueryHandler(IChatMessageRepository repo)
    : IRequestHandler<GetChatHistoryQuery, List<ChatMessageDto>>
{
    public async Task<List<ChatMessageDto>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var messages = await repo.GetByUserIdAsync(request.UserId);
        return messages.ConvertAll(ChatMessageDto.FromEntity);
    }
}
