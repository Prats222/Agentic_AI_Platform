using AgenticPlatform.Core.DTOs.Agents;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class AgentMappingProfile : Profile
{
    public AgentMappingProfile()
    {
        CreateMap<Agent, AgentDto>();
        CreateMap<CreateAgentDto, Agent>();
        CreateMap<UpdateAgentDto, Agent>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.Tools, options => options.Ignore())
            .ForMember(destination => destination.Workflows, options => options.Ignore())
            .ForMember(destination => destination.WorkflowSteps, options => options.Ignore())
            .ForMember(destination => destination.Executions, options => options.Ignore());
    }
}
