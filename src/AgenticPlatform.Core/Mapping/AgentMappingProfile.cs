using AgenticPlatform.Core.DTOs.Agents;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class AgentMappingProfile : Profile
{
    public AgentMappingProfile()
    {
        CreateMap<Agent, AgentDto>()
            .ForMember(
                destination => destination.HasAIApiKey,
                options => options.MapFrom(source => !string.IsNullOrWhiteSpace(source.AIApiKey)))
            .ForMember(
                destination => destination.ToolIds,
                options => options.MapFrom(source => source.Tools.Select(tool => tool.Id)))
            .ForMember(
                destination => destination.ToolNames,
                options => options.MapFrom(source => source.Tools.Select(tool => tool.Name)))
            .ForMember(
                destination => destination.ContextDocumentIds,
                options => options.MapFrom(source => source.ContextDocuments.Select(document => document.Id)))
            .ForMember(
                destination => destination.ContextDocumentNames,
                options => options.MapFrom(source => source.ContextDocuments.Select(document => document.Name)));
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
