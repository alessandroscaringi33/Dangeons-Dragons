using DndCompanion.Core.Dice;
using DndCompanion.Core.Domain.Entities;
using DndCompanion.Core.Domain.Enums;
using DndCompanion.Core.SkillChecks;
using DndCompanion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.SkillChecks;

/// <summary>
/// Records skill checks into the active session of a campaign. A check is
/// persisted as a <see cref="SkillCheck"/> and also added to the session
/// timeline as a <see cref="SessionEvent"/> of type <see cref="SessionEventType.SkillCheck"/>.
/// </summary>
public sealed class SkillCheckService : ISkillCheckService
{
    private readonly ICampaignDatabaseFactory _databaseFactory;
    private readonly IDiceService _diceService;
    private readonly ILogger<SkillCheckService> _logger;

    public SkillCheckService(
        ICampaignDatabaseFactory databaseFactory,
        IDiceService diceService,
        ILogger<SkillCheckService> logger)
    {
        _databaseFactory = databaseFactory;
        _diceService = diceService;
        _logger = logger;
    }

    public async Task<SkillCheckInfo> RecordCheckAsync(
        string campaignFolderPath,
        SkillCheckDraft draft,
        CancellationToken cancellationToken = default)
    {
        Validate(draft);

        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var campaign = await context.Campaigns
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SkillCheckException("La campagna non è stata trovata o non è inizializzata.");

        var session = await GetOrCreateSessionAsync(context, campaign, cancellationToken).ConfigureAwait(false);

        var diceResult = ResolveRoll(draft);

        var check = new SkillCheck
        {
            SessionId = session.Id,
            CharacterId = draft.CharacterId,
            Skill = draft.Skill.Trim(),
            Roll = diceResult,
            Modifier = draft.Modifier,
            DifficultyClass = draft.DifficultyClass,
            IsPhysicalRoll = draft.IsPhysicalRoll,
            Timestamp = DateTime.UtcNow
        };

        context.SkillChecks.Add(check);

        var characterName = draft.CharacterId is null
            ? null
            : await GetCharacterNameAsync(context, draft.CharacterId.Value, cancellationToken).ConfigureAwait(false);

        context.SessionEvents.Add(new SessionEvent
        {
            SessionId = session.Id,
            Timestamp = check.Timestamp,
            Type = SessionEventType.SkillCheck,
            Description = BuildEventDescription(check, characterName),
            RelatedCharacterId = draft.CharacterId
        });

        await SaveAsync(context, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Recorded skill check '{Skill}' (CD {Dc}) total {Total} -> {Outcome} in campaign {Folder}",
            check.Skill,
            check.DifficultyClass,
            check.Total,
            check.IsSuccess ? "success" : "failure",
            campaignFolderPath);

        return ToInfo(check, characterName);
    }

    public async Task<IReadOnlyList<SkillCheckInfo>> ListChecksAsync(
        string campaignFolderPath,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = _databaseFactory.CreateContext(campaignFolderPath);

        var checks = await context.SkillChecks
            .AsNoTracking()
            .OrderByDescending(s => s.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<SkillCheckInfo>(checks.Count);
        foreach (var check in checks)
        {
            var characterName = check.CharacterId is null
                ? null
                : await GetCharacterNameAsync(context, check.CharacterId.Value, cancellationToken).ConfigureAwait(false);
            result.Add(ToInfo(check, characterName));
        }

        return result;
    }

    private int ResolveRoll(SkillCheckDraft draft)
    {
        if (draft.IsPhysicalRoll)
        {
            if (draft.PhysicalRoll is null)
            {
                throw new SkillCheckException("Per un tiro fisico è necessario inserire il valore del dado.");
            }

            try
            {
                return _diceService.RollPhysical(draft.DiceNotation, draft.PhysicalRoll.Value).KeptResult;
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
            {
                throw new SkillCheckException(ex.Message, ex);
            }
        }

        try
        {
            return _diceService.Roll(draft.DiceNotation).KeptResult;
        }
        catch (DiceParseException ex)
        {
            throw new SkillCheckException(ex.Message, ex);
        }
    }

    private static async Task<Session> GetOrCreateSessionAsync(
        CampaignDbContext context,
        Campaign campaign,
        CancellationToken cancellationToken)
    {
        if (campaign.ActiveSessionId is Guid activeId)
        {
            var active = campaign.Sessions.FirstOrDefault(s => s.Id == activeId);
            if (active is not null && active.IsActive)
            {
                return active;
            }
        }

        var nextNumber = (await context.Sessions.CountAsync(s => s.CampaignId == campaign.Id, cancellationToken).ConfigureAwait(false)) + 1;

        var session = new Session
        {
            CampaignId = campaign.Id,
            Number = nextNumber,
            Title = $"Sessione {nextNumber}"
        };
        session.Start();

        campaign.ActiveSessionId = session.Id;
        campaign.Touch();

        context.Sessions.Add(session);
        return session;
    }

    private static async Task<string?> GetCharacterNameAsync(
        CampaignDbContext context,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        return await context.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static void Validate(SkillCheckDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Skill))
        {
            throw new SkillCheckException("L'abilità è obbligatoria.");
        }

        if (draft.DifficultyClass < 0)
        {
            throw new SkillCheckException("La CD non può essere negativa.");
        }

        if (draft.IsPhysicalRoll && draft.PhysicalRoll is null)
        {
            throw new SkillCheckException("Per un tiro fisico è necessario inserire il valore del dado.");
        }
    }

    private static string BuildEventDescription(SkillCheck check, string? characterName)
    {
        var who = string.IsNullOrWhiteSpace(characterName) ? "Il gruppo" : characterName;
        var outcome = check.IsSuccess ? "SUCCESSO" : "FALLIMENTO";
        return $"{who} — {check.Skill}: tiro {check.Roll}, bonus {check.Modifier:+#;-#;0}, " +
               $"totale {check.Total} (CD {check.DifficultyClass}) — {outcome}";
    }

    private static async Task SaveAsync(CampaignDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SkillCheckException("Non è stato possibile registrare il tiro.", ex);
        }
    }

    private static SkillCheckInfo ToInfo(SkillCheck check, string? characterName)
    {
        return new SkillCheckInfo
        {
            Id = check.Id,
            SessionId = check.SessionId,
            CharacterId = check.CharacterId,
            CharacterName = characterName,
            Skill = check.Skill,
            Roll = check.Roll,
            Modifier = check.Modifier,
            DifficultyClass = check.DifficultyClass,
            IsPhysicalRoll = check.IsPhysicalRoll,
            Timestamp = check.Timestamp,
            Total = check.Total,
            IsSuccess = check.IsSuccess
        };
    }
}