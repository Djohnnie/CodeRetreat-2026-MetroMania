using MediatR;
using MetroMania.Domain.Extensions;
using MetroMania.Scripting;

namespace MetroMania.Application.Submissions.Queries;

public record GetStarterCodeQuery : IRequest<string>;

public class GetStarterCodeQueryHandler : IRequestHandler<GetStarterCodeQuery, string>
{
    public Task<string> Handle(GetStarterCodeQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(StarterCode.Template.Base64Encode());
    }
}
