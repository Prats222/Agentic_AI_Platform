using AgenticPlatform.Core.DTOs.Executions;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class ExecutionMappingProfile : Profile
{
    public ExecutionMappingProfile()
    {
        CreateMap<Execution, ExecutionDto>();
        CreateMap<ExecutionLog, ExecutionLogDto>();
    }
}
