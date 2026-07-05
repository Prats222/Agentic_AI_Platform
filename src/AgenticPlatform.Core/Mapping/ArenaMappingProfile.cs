using AgenticPlatform.Core.DTOs.Arena;
using AgenticPlatform.Core.Entities;
using AutoMapper;

namespace AgenticPlatform.Core.Mapping;

public sealed class ArenaMappingProfile : Profile
{
    public ArenaMappingProfile()
    {
        CreateMap<ArenaChallenge, ArenaChallengeDto>();
        CreateMap<ArenaEntry, ArenaEntryDto>();
    }
}
