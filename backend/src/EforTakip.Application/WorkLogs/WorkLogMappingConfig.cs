using EforTakip.Application.WorkLogs.Dtos;
using EforTakip.Domain.WorkLogs;
using Mapster;

namespace EforTakip.Application.WorkLogs;

public sealed class WorkLogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EmployeeWorkLog, EmployeeWorkLogDto>()
            .Map(dest => dest.IsApproved, src => src.ApprovalId != null);
    }
}
