using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Auth.Queries;

public record LoginQuery(string Name, string Password) : IRequest<LoginResult>;

public record LoginResult(bool Success, string? Error, UserDto? User);

public class LoginQueryHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    : IRequestHandler<LoginQuery, LoginResult>
{
    public async Task<LoginResult> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByNameAsync(request.Name);
        if (user is null)
            return new LoginResult(false, "Invalid username or password.", null);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return new LoginResult(false, "Invalid username or password.", null);

        if (user.ApprovalStatus != ApprovalStatus.Approved)
            return new LoginResult(false, "Your account is pending approval by an administrator.", null);

        var dto = new UserDto(user.Id, user.Name, user.Role, user.ApprovalStatus, user.IsDarkMode, user.CreatedAt);
        return new LoginResult(true, null, dto);
    }
}
