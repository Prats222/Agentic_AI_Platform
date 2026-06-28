using AgenticPlatform.Core.DTOs.Tools;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class ToolMappingProfile : Profile
{
    public ToolMappingProfile()
    {
        CreateMap<Tool, ToolDto>();
        CreateMap<CreateToolDto, Tool>();
        CreateMap<UpdateToolDto, Tool>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.Agents, options => options.Ignore())
            .ForMember(destination => destination.WorkflowSteps, options => options.Ignore());
    }
}
