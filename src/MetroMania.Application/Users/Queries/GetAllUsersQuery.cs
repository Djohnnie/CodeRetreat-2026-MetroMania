using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Interfaces;

namespace MetroMania.Application.Users.Queries;

public record GetAllUsersQuery : IRequest<List<UserDto>>;

public class GetAllUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetAllUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync();
        return users.Select(u => new UserDto(u.Id, u.Name, u.Role, u.ApprovalStatus, u.IsDarkMode, u.CreatedAt)).ToList();
    }
}
