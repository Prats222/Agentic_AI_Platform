using AgenticPlatform.Core.DTOs.HumanApprovals;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class HumanApprovalMappingProfile : Profile
{
    public HumanApprovalMappingProfile()
    {
        CreateMap<HumanApprovalRequest, HumanApprovalRequestDto>();
    }
}
