using AgenticPlatform.Core.DTOs.Workflows;
using AgenticPlatform.Core.DTOs.WorkflowSteps;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class WorkflowMappingProfile : Profile
{
    public WorkflowMappingProfile()
    {
        CreateMap<Workflow, WorkflowDto>();
        CreateMap<CreateWorkflowDto, Workflow>();
        CreateMap<UpdateWorkflowDto, Workflow>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.Steps, options => options.Ignore())
            .ForMember(destination => destination.Agents, options => options.Ignore())
            .ForMember(destination => destination.Executions, options => options.Ignore());

        CreateMap<WorkflowStep, WorkflowStepDto>();
        CreateMap<CreateWorkflowStepDto, WorkflowStep>();
        CreateMap<UpdateWorkflowStepDto, WorkflowStep>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.WorkflowId, options => options.Ignore())
            .ForMember(destination => destination.Workflow, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.Tool, options => options.Ignore())
            .ForMember(destination => destination.Agent, options => options.Ignore());
    }
}
