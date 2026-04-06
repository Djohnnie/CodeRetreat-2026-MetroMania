using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Conductor.Queries;

public record GetFullChatHistoryQuery(Guid UserId) : IRequest<List<ChatMessageDto>>;

public class GetFullChatHistoryQueryHandler(IChatMessageRepository repo)
    : IRequestHandler<GetFullChatHistoryQuery, List<ChatMessageDto>>
{
    public async Task<List<ChatMessageDto>> Handle(GetFullChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var messages = await repo.GetAllByUserIdAsync(request.UserId);
        return messages.ConvertAll(ChatMessageDto.FromEntity);
    }
}
