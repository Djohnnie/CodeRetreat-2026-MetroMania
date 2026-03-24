using MediatR;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Language.Commands;

public record ChangeLanguageCommand(Guid UserId, string Language) : IRequest<bool>;

public class ChangeLanguageCommandHandler(IUserRepository userRepository)
    : IRequestHandler<ChangeLanguageCommand, bool>
{
    private static readonly HashSet<string> SupportedLanguages = ["en", "nl"];

    public async Task<bool> Handle(ChangeLanguageCommand request, CancellationToken cancellationToken)
    {
        if (!SupportedLanguages.Contains(request.Language))
            return false;

        var user = await userRepository.GetByIdAsync(request.UserId);
        if (user is null) return false;

        user.Language = request.Language;
        await userRepository.UpdateAsync(user);
        return true;
    }
}
