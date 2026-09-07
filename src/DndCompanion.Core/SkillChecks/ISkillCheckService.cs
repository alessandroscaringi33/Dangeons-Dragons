namespace DndCompanion.Core.SkillChecks;

/// <summary>
/// Application service that records skill checks into the active session of a
/// campaign. It uses the campaign's active session when present, otherwise it
/// starts a new one. Every method operates on a single campaign identified by
/// its folder path.
/// </summary>
public interface ISkillCheckService
{
    /// <summary>
    /// Records a new skill check. The roll is generated from the dice notation
    /// or, when <see cref="SkillCheckDraft.IsPhysicalRoll"/> is true, uses the
    /// provided physical value verbatim.
    /// </summary>
    /// <exception cref="SkillCheckException">When the check cannot be recorded.</exception>
    Task<SkillCheckInfo> RecordCheckAsync(
        string campaignFolderPath,
        SkillCheckDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the recent skill checks of the active session (or of the
    /// latest session of the campaign).</summary>
    Task<IReadOnlyList<SkillCheckInfo>> ListChecksAsync(
        string campaignFolderPath,
        int limit = 50,
        CancellationToken cancellationToken = default);
}