using MediatR;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Users.Commands;

public record ApproveUserCommand(Guid UserId, ApprovalStatus NewStatus) : IRequest<bool>;

public class ApproveUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<ApproveUserCommand, bool>
{
    public async Task<bool> Handle(ApproveUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId);
        if (user is null) return false;

        user.ApprovalStatus = request.NewStatus;
        await userRepository.UpdateAsync(user);
        return true;
    }
}
