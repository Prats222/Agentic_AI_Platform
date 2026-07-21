using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using AgenticPlatform.API.Realms;
using AgenticPlatform.API.Extensions;
using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Arena;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Asp.Versioning;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
[Route("api/v{version:apiVersion}/arena")]
public sealed class ArenaController : ControllerBase
{
    private const int ArenaEntryMaxTokens = 1200;
    private const int ArenaJudgeMaxTokens = 700;
    private const int ArenaJudgeEntryMaxCharacters = 2400;
    private const string GroqArenaJudgeModel = "meta-llama/llama-4-scout-17b-16e-instruct";
    private readonly ApplicationDbContext _dbContext;
    private readonly IAISettingsService _aiSettingsService;
    private readonly ILLMProviderFactory _llmProviderFactory;
    private readonly IMapper _mapper;

    public ArenaController(
        ApplicationDbContext dbContext,
        IAISettingsService aiSettingsService,
        ILLMProviderFactory llmProviderFactory,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _aiSettingsService = aiSettingsService;
        _llmProviderFactory = llmProviderFactory;
        _mapper = mapper;
    }

    [HttpGet("challenges")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ArenaChallengeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ArenaChallengeDto>>>> GetChallenges(CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var challenges = await _dbContext.ArenaChallenges
            .AsNoTracking()
            .Where(challenge => challenge.RealmId == realmId)
            .Include(challenge => challenge.Entries.OrderByDescending(entry => entry.Score))
            .OrderByDescending(challenge => challenge.CreatedAt)
            .Take(50)
            .ProjectTo<ArenaChallengeDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<ArenaChallengeDto>>.Ok(challenges));
    }

    [HttpPost("challenges")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ArenaChallengeDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ArenaChallengeDto>>> CreateChallenge(CreateArenaChallengeDto request, CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var challenge = new ArenaChallenge
        {
            RealmId = realmId,
            CreatedByUserId = GetCurrentUserId(),
            CreatedByDisplayName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "Arena Creator",
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            TaskPrompt = request.TaskPrompt.Trim(),
            Rules = request.Rules.Trim(),
            ExpectedOutput = request.ExpectedOutput.Trim(),
            JudgeCriteria = request.JudgeCriteria.Trim(),
            Status = ArenaChallengeStatus.Open
        };

        await _dbContext.ArenaChallenges.AddAsync(challenge, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetChallenges), ApiResponse<ArenaChallengeDto>.Ok(_mapper.Map<ArenaChallengeDto>(challenge), "Arena challenge created."));
    }

    [HttpPost("challenges/{challengeId:guid}/entries")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ArenaChallengeDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ArenaChallengeDto>>> SubmitEntry(Guid challengeId, CreateArenaEntryDto request, CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var challenge = await _dbContext.ArenaChallenges
            .Include(item => item.Entries)
            .FirstOrDefaultAsync(item => item.Id == challengeId && item.RealmId == realmId, cancellationToken);

        if (challenge is null)
        {
            return NotFound(ApiResponse<ArenaChallengeDto>.Fail("Arena challenge was not found."));
        }

        var agent = await _dbContext.Agents
            .AsNoTracking()
            .VisibleTo(User.GetUserId(), User.IsAdmin())
            .FirstOrDefaultAsync(item => item.Id == request.AgentId && item.RealmId == realmId, cancellationToken);

        if (agent is null)
        {
            return NotFound(ApiResponse<ArenaChallengeDto>.Fail("Agent was not found in the selected realm."));
        }

        if (challenge.Entries.Any(entry => entry.AgentId == agent.Id))
        {
            return Conflict(ApiResponse<ArenaChallengeDto>.Fail("This agent is already entered in the challenge."));
        }

        var entry = new ArenaEntry
        {
            ChallengeId = challenge.Id,
            SubmittedByUserId = GetCurrentUserId(),
            SubmittedByDisplayName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "Arena Challenger",
            AgentId = agent.Id,
            AgentName = agent.Name
        };

        await _dbContext.ArenaEntries.AddAsync(entry, cancellationToken);

        challenge.Status = ArenaChallengeStatus.Open;
        challenge.WinnerEntryId = null;
        challenge.JudgeSummary = null;
        challenge.ScorecardJson = null;
        challenge.CompletedAt = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        var refreshed = await _dbContext.ArenaChallenges
            .AsNoTracking()
            .Include(item => item.Entries.OrderByDescending(arenaEntry => arenaEntry.Score))
            .FirstAsync(item => item.Id == challengeId && item.RealmId == realmId, cancellationToken);

        return CreatedAtAction(nameof(GetChallenges), ApiResponse<ArenaChallengeDto>.Ok(_mapper.Map<ArenaChallengeDto>(refreshed), "Agent entered the arena."));
    }

    [HttpPost("challenges/{challengeId:guid}/run")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ArenaChallengeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ArenaChallengeDto>>> RunBattle(Guid challengeId, CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var challenge = await _dbContext.ArenaChallenges
            .Include(item => item.Entries)
            .FirstOrDefaultAsync(item => item.Id == challengeId && item.RealmId == realmId, cancellationToken);

        if (challenge is null)
        {
            return NotFound(ApiResponse<ArenaChallengeDto>.Fail("Arena challenge was not found."));
        }

        if (challenge.Entries.Count < 2)
        {
            return BadRequest(ApiResponse<ArenaChallengeDto>.Fail("At least two agents must enter before a battle can run."));
        }

        challenge.Status = ArenaChallengeStatus.Running;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            foreach (var entry in challenge.Entries)
            {
                await RunEntryAsync(challenge, entry, cancellationToken);
            }

            var judge = await JudgeBattleWithFallbackAsync(challenge, cancellationToken);
            ApplyJudgeVerdict(challenge, judge);
            challenge.Status = ArenaChallengeStatus.Completed;
            challenge.CompletedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            challenge.Status = ArenaChallengeStatus.Failed;
            challenge.JudgeSummary = ex.Message;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var refreshed = await _dbContext.ArenaChallenges
            .AsNoTracking()
            .Include(item => item.Entries.OrderByDescending(entry => entry.Score))
            .FirstAsync(item => item.Id == challenge.Id, cancellationToken);

        return Ok(ApiResponse<ArenaChallengeDto>.Ok(_mapper.Map<ArenaChallengeDto>(refreshed), "Arena battle completed."));
    }

    private async Task RunEntryAsync(ArenaChallenge challenge, ArenaEntry entry, CancellationToken cancellationToken)
    {
        var prompt = $"""
Arena Challenge: {challenge.Title}

Task:
{challenge.TaskPrompt}

Rules:
{challenge.Rules}

Expected Output:
{challenge.ExpectedOutput}

Return your best final answer. Follow the rules exactly.
""";

        var request = await _aiSettingsService.BuildChatRequestAsync(entry.AgentId, prompt, cancellationToken)
            ?? throw new InvalidOperationException("Agent could not be loaded for arena battle.");

        request.MaxTokens = Math.Min(request.MaxTokens, ArenaEntryMaxTokens);

        var provider = _llmProviderFactory.GetProvider(request.Provider);
        var stopwatch = Stopwatch.StartNew();
        var response = await provider.ChatAsync(request, cancellationToken);
        stopwatch.Stop();

        entry.Output = response.Content;
        entry.DurationMs = stopwatch.Elapsed.TotalMilliseconds;
        entry.Provider = request.Provider.ToString();
        entry.Model = request.Model;
    }

    private async Task<ArenaJudgeVerdict> JudgeBattleWithFallbackAsync(ArenaChallenge challenge, CancellationToken cancellationToken)
    {
        try
        {
            return await JudgeBattleAsync(challenge, cancellationToken);
        }
        catch (Exception ex)
        {
            return BuildFallbackVerdict(challenge, ex.Message);
        }
    }

    private async Task<ArenaJudgeVerdict> JudgeBattleAsync(ArenaChallenge challenge, CancellationToken cancellationToken)
    {
        var entries = string.Join(
            Environment.NewLine + Environment.NewLine,
            challenge.Entries.Select(entry => $"""
EntryId: {entry.Id}
Agent: {entry.AgentName}
Creator: {entry.SubmittedByDisplayName}
Output:
{TruncateForJudge(entry.Output)}
"""));

        var judgePrompt = $$"""
You are the PratsPilot Arena judge. Compare all entries against the challenge.

Challenge: {{challenge.Title}}
Description: {{challenge.Description}}
Task: {{challenge.TaskPrompt}}
Rules: {{challenge.Rules}}
Expected Output: {{challenge.ExpectedOutput}}
Judge Criteria: {{challenge.JudgeCriteria}}

Entries:
{{entries}}

Return only JSON in this exact shape:
{
  "winnerEntryId": "guid",
  "summary": "short verdict",
  "scores": [
    {
      "entryId": "guid",
      "score": 0-10,
      "feedback": "why this score"
    }
  ]
}
""";

        var request = await _aiSettingsService.BuildChatRequestAsync(null, judgePrompt, cancellationToken);
        request!.MaxTokens = Math.Min(request.MaxTokens, ArenaJudgeMaxTokens);
        request.Temperature = Math.Min(request.Temperature, 0.2);

        // Groq applies token limits per model. Arena entries may consume most of the
        // 8B model's 6K TPM budget, so use its higher-throughput free Scout model to judge.
        if (request.Provider == AIProvider.Groq)
        {
            request.Model = GroqArenaJudgeModel;
            request.BaseUrl = "https://api.groq.com/openai/v1";
        }

        var provider = _llmProviderFactory.GetProvider(request!.Provider);
        var response = await provider.ChatAsync(request, cancellationToken);
        var json = ExtractJson(response.Content);
        return JsonSerializer.Deserialize<ArenaJudgeVerdict>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Arena judge returned an empty verdict.");
    }

    private static void ApplyJudgeVerdict(ArenaChallenge challenge, ArenaJudgeVerdict verdict)
    {
        challenge.WinnerEntryId = verdict.WinnerEntryId;
        challenge.JudgeSummary = verdict.Summary;
        challenge.ScorecardJson = JsonSerializer.Serialize(verdict);

        foreach (var score in verdict.Scores)
        {
            var entry = challenge.Entries.FirstOrDefault(item => item.Id == score.EntryId);
            if (entry is null)
            {
                continue;
            }

            entry.Score = score.Score;
            entry.Feedback = score.Feedback;
        }
    }

    private static ArenaJudgeVerdict BuildFallbackVerdict(ArenaChallenge challenge, string judgeError)
    {
        var safeError = SummarizeProviderError(judgeError);
        var scores = challenge.Entries
            .Select(entry =>
            {
                var score = ScoreOutputHeuristically(entry.Output, challenge);
                return new ArenaJudgeScore
                {
                    EntryId = entry.Id,
                    Score = score,
                    Feedback = $"Fallback judge used because the LLM judge was unavailable ({safeError}). The score considers output completeness, code structure, rule following, and formatting."
                };
            })
            .OrderByDescending(score => score.Score)
            .ToList();

        return new ArenaJudgeVerdict
        {
            WinnerEntryId = scores.FirstOrDefault()?.EntryId ?? Guid.Empty,
            Summary = $"The LLM judge was temporarily unavailable ({safeError}), so PratsPilot completed the battle using deterministic fallback scoring.",
            Scores = scores
        };
    }

    private static double ScoreOutputHeuristically(string? output, ArenaChallenge challenge)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return 0;
        }

        var score = 4.0;
        var normalized = output.Trim();

        if (normalized.Contains("import ", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("def ", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("class ", StringComparison.OrdinalIgnoreCase))
        {
            score += 1.5;
        }

        if (normalized.Length is >= 120 and <= 4000)
        {
            score += 1.0;
        }

        if (!normalized.Contains("```", StringComparison.Ordinal))
        {
            score += 1.0;
        }

        if (challenge.Rules.Contains("code only", StringComparison.OrdinalIgnoreCase)
            && !LooksLikeExplanation(normalized))
        {
            score += 1.0;
        }

        if (normalized.Contains("try", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("except", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("if ", StringComparison.OrdinalIgnoreCase))
        {
            score += 1.0;
        }

        if (normalized.EndsWith(",", StringComparison.Ordinal)
            || normalized.EndsWith("(", StringComparison.Ordinal)
            || normalized.EndsWith("[", StringComparison.Ordinal)
            || normalized.EndsWith("{", StringComparison.Ordinal))
        {
            score -= 2.0;
        }

        return Math.Clamp(Math.Round(score, 1), 0, 10);
    }

    private static bool LooksLikeExplanation(string value)
    {
        return value.Contains("here is", StringComparison.OrdinalIgnoreCase)
            || value.Contains("this code", StringComparison.OrdinalIgnoreCase)
            || value.Contains("explanation", StringComparison.OrdinalIgnoreCase)
            || value.Contains("you can", StringComparison.OrdinalIgnoreCase);
    }

    private static string TruncateForJudge(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "[No output]";
        }

        var normalized = value.Trim();
        return normalized.Length <= ArenaJudgeEntryMaxCharacters
            ? normalized
            : $"{normalized[..ArenaJudgeEntryMaxCharacters]}\n[Output truncated for quota-efficient judging]";
    }

    private static string SummarizeProviderError(string value)
    {
        if (value.Contains("429", StringComparison.OrdinalIgnoreCase))
        {
            return "provider rate limit reached";
        }

        if (value.Contains("404", StringComparison.OrdinalIgnoreCase))
        {
            return "configured judge model unavailable";
        }

        return "provider request failed";
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
    }

    private static string ExtractJson(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return value[start..(end + 1)];
        }

        return value;
    }

    private sealed class ArenaJudgeVerdict
    {
        public Guid WinnerEntryId { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<ArenaJudgeScore> Scores { get; set; } = [];
    }

    private sealed class ArenaJudgeScore
    {
        public Guid EntryId { get; set; }
        public double Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
}
