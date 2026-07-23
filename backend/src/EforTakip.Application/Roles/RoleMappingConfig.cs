using EforTakip.Application.Roles.Dtos;
using EforTakip.Domain.Roles;
using Mapster;

namespace EforTakip.Application.Roles;

public sealed class RoleMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Role, RoleDto>()
            .Map(dest => dest.PermissionCount, src => src.Permissions.Count);
    }
}
