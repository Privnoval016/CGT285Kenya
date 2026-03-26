using UnityEngine;

namespace Configuration
{
    /**
     * <summary>
     * MatchRulesConfig defines configurable match parameters.
     * Use this to tune game duration, early-end conditions, and other match rules.
     * </summary>
     */
    [CreateAssetMenu(menuName = "Game/Match Rules Config", fileName = "MatchRulesConfig")]
    public class MatchRulesConfig : ScriptableObject
{
    [Header("Match Duration")]
    [Tooltip("Total match duration in seconds")]
    [SerializeField] private float matchDurationSeconds = 300f;

    [Header("Early End Condition")]
    [Tooltip("Time at which to start checking point lead condition (in seconds). Set to 0 to disable.")]
    [SerializeField] private float earlyEndCheckTimeSeconds = 120f;

    [Tooltip("Point lead required to end match early. Set to 0 to disable.")]
    [SerializeField] private int pointLeadToEndEarly = 3;

    [Header("Score to Win")]
    [Tooltip("Maximum score needed to win (fallback if early end doesn't trigger)")]
    [SerializeField] private int scoreToWin = 10;

    [Header("Post-Score Delay")]
    [Tooltip("Delay after a goal before resetting the field (in seconds)")]
    [SerializeField] private float postScoreResetDelay = 0.5f;

    public float MatchDurationSeconds => matchDurationSeconds;
    public float EarlyEndCheckTimeSeconds => earlyEndCheckTimeSeconds;
    public int PointLeadToEndEarly => pointLeadToEndEarly;
    public int ScoreToWin => scoreToWin;
    public float PostScoreResetDelay => postScoreResetDelay;

    /**
     * <summary>
     * Checks if match should end early based on current scores and elapsed time.
     * </summary>
     * <param name="team0Score">Team 0 current score</param>
     * <param name="team1Score">Team 1 current score</param>
     * <param name="elapsedTimeSeconds">Time elapsed since match start</param>
     * <returns>True if early end condition is met</returns>
     */
    public bool ShouldEndEarly(int team0Score, int team1Score, float elapsedTimeSeconds)
    {
        if (earlyEndCheckTimeSeconds <= 0f || pointLeadToEndEarly <= 0)
        {
            return false;
        }

        if (elapsedTimeSeconds < earlyEndCheckTimeSeconds)
        {
            return false;
        }

        int scoreDiff = Mathf.Abs(team0Score - team1Score);
        return scoreDiff >= pointLeadToEndEarly;
    }
}
}



