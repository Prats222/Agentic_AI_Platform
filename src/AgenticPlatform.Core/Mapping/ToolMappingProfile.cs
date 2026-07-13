using AgenticPlatform.Core.DTOs.Tools;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class ToolMappingProfile : Profile
{
    public ToolMappingProfile()
    {
        CreateMap<Tool, ToolDto>()
            .ForMember(
                destination => destination.HasSecrets,
                options => options.MapFrom(source => !string.IsNullOrWhiteSpace(source.SecretJson) && source.SecretJson != "{}"));
        CreateMap<CreateToolDto, Tool>();
        CreateMap<UpdateToolDto, Tool>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.CreatedByUserId, options => options.Ignore())
            .ForMember(destination => destination.CreatedByDisplayName, options => options.Ignore())
            .ForMember(destination => destination.SecretJson, options => options.Condition(source => source.SecretJson is not null))
            .ForMember(destination => destination.Agents, options => options.Ignore())
            .ForMember(destination => destination.WorkflowSteps, options => options.Ignore());
    }
}
