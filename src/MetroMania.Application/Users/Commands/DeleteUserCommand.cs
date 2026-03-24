using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Users.Commands;

public record DeleteUserCommand(Guid UserId) : IRequest<bool>;

public class DeleteUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId);
        if (user is null) return false;

        await userRepository.DeleteAsync(request.UserId);
        return true;
    }
}
