using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Auth.Commands;

public record RegisterCommand(string Name, string Password) : IRequest<RegisterResult>;

public record RegisterResult(bool Success, string? Error, UserDto? User);

public class RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByNameAsync(request.Name);
        if (existing is not null)
            return new RegisterResult(false, "A user with that name already exists.", null);

        var isFirstUser = await userRepository.CountAsync() == 0;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = isFirstUser ? UserRole.Admin : UserRole.User,
            ApprovalStatus = isFirstUser ? ApprovalStatus.Approved : ApprovalStatus.Pending,
            IsDarkMode = false
        };

        await userRepository.AddAsync(user);

        var dto = new UserDto(user.Id, user.Name, user.Role, user.ApprovalStatus, user.IsDarkMode, user.CreatedAt);
        return new RegisterResult(true, null, dto);
    }
}
