using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class Standings
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public Guid TeamId { get; set; }

    public string? PlayoffStatus { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int OTLosses { get; set; }

    public decimal DivisionGamesBehind { get; set; }

    public int GoalsFor { get; set; }

    public int GoalsAgainst { get; set; }

    public int Streak { get; set; }

    public int WinsVsDivision { get; set; }

    public int LossesVsDivision { get; set; }

    public int OTLossesVsDivision { get; set; }

    public int WinsVsConference { get; set; }

    public int LossesVsConference { get; set; }

    public int OTLossesVsConference { get; set; }

    public int InterConfWins { get; set; }

    public int InterConfLosses { get; set; }

    public int InterConfOTLosses { get; set; }

    public int RegPlusOTWins { get; set; }

    public int RegulationWins { get; set; }

    public Guid? ConferenceId { get; set; }

    public Guid? DivisionId { get; set; }

    public int GamesPlayed { get; set; }

    public int GamesPlayedVsConference { get; set; }

    public int GamesPlayedVsDivision { get; set; }

    public int InterConfGamesPlayed { get; set; }

    public decimal InterConfWinPct { get; set; }

    public int Points { get; set; }

    public decimal PointsPct { get; set; }

    public decimal WinPct { get; set; }

    public decimal WinPctVsConference { get; set; }

    public decimal WinPctVsDivision { get; set; }

    public int MaximumPossiblePoints { get; set; }

    public int GoalDifferential { get; set; }

    public decimal ConferenceGamesBehind { get; set; }

    public decimal LeagueGamesBehind { get; set; }

    public int StreakGoalDifferential { get; set; }

    public int StreakGoalsAgainst { get; set; }

    public int StreakGoalsFor { get; set; }

    public int GamesPlayedInLast10Games { get; set; }

    public int LossesInLast10Games { get; set; }

    public decimal WinPctInLast10Games { get; set; }

    public int WinsInLast10Games { get; set; }

    public decimal PlayoffsGamesBehind { get; set; }

    public virtual Conference? Conference { get; set; }

    public virtual Division? Division { get; set; }

    public virtual Season Season { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
