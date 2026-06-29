using AgenticPlatform.Core.DTOs.AI;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class AISettingsMappingProfile : Profile
{
    public AISettingsMappingProfile()
    {
        CreateMap<AISettings, AISettingsDto>()
            .ForMember(
                destination => destination.HasApiKey,
                options => options.MapFrom(source => !string.IsNullOrWhiteSpace(source.ApiKey)));
        CreateMap<UpdateAISettingsDto, AISettings>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore());
    }
}
