using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Setup.Queries.GetSetupStatus;

public class GetSetupStatusQueryHandler : IRequestHandler<GetSetupStatusQuery, Result<SetupStatusResponse>>
{
    private readonly ISetupCodeService _setupCodeService;

    public GetSetupStatusQueryHandler(ISetupCodeService setupCodeService)
    {
        _setupCodeService = setupCodeService;
    }

    public async Task<Result<SetupStatusResponse>> Handle(GetSetupStatusQuery request, CancellationToken cancellationToken)
    {
        var isRequired = await _setupCodeService.IsSetupRequiredAsync(cancellationToken);
        return Result<SetupStatusResponse>.Success(new SetupStatusResponse(isRequired));
    }
}
