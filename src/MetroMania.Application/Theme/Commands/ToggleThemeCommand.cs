using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Theme.Commands;

public record ToggleThemeCommand(Guid UserId) : IRequest<bool>;

public class ToggleThemeCommandHandler(IUserRepository userRepository)
    : IRequestHandler<ToggleThemeCommand, bool>
{
    public async Task<bool> Handle(ToggleThemeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId);
        if (user is null) return false;

        user.IsDarkMode = !user.IsDarkMode;
        await userRepository.UpdateAsync(user);
        return user.IsDarkMode;
    }
}
