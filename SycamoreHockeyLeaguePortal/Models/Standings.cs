using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Standings")]
    public class Standings
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        [Display(Name = "Season")]
        public Season Season { get; set; }

        [ForeignKey(nameof(Conference))]
        public Guid? ConferenceId { get; set; }

        [Display(Name = "Conference")]
        public Conference? Conference { get; set; }

        [ForeignKey(nameof(Division))]
        public Guid? DivisionId { get; set; }

        [Display(Name = "Division")]
        public Division? Division { get; set; }

        [Display(Name = "Div. Ranking")]
        public int DivisionRanking { get; set; }

        [Display(Name = "Conf. Ranking")]
        public int ConferenceRanking { get; set; }
        
        [Display(Name = "Playoff Ranking")]
        public int PlayoffRanking { get; set; }

        [Display(Name = "League Ranking")]
        public int LeagueRanking { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid TeamId { get; set; }

        [Display(Name = "Team")]
        public Team Team { get; set; }

        [Display(Name = "PS")]
        public string? PlayoffStatus { get; set; }

        [Display(Name = "GP")]
        public int GamesPlayed { get; set; }

        [Display(Name = "W")]
        public int Wins { get; set; }

        [Display(Name = "OTW")]
        public int OTWins { get; set; }

        [Display(Name = "OTL")]
        public int OTLosses { get; set; }

        [Display(Name = "L")]
        public int Losses { get; set; }

        [Display(Name = "W-L-OTL")]
        public string Record_2021Format => $"{Wins}-{Losses}-{OTLosses}";
        
        [Display(Name = "W-L")]
        public string Record_2024Format => $"{Wins}-{Losses}";

        [Display(Name = "W-OTW-OTL-L")]
        public string Record_2026Format => $"{Wins}-{OTWins}-{OTLosses}-{Losses}";

        [Display(Name = "RW")]
        public int RegulationWins { get; set; }

        [Display(Name = "ROW")]
        public int RegPlusOTWins { get; set; }

        [Display(Name = "Pts.")]
        public int Points { get; set; }

        [Display(Name = "PC")]
        public int PointsCeiling { get; set; }

        [Column(TypeName = "decimal(4,3)")]
        [Display(Name = "W%")]
        public decimal WinPct { get; set; }

        [Column(TypeName = "decimal(4,3)")]
        [Display(Name = "P%")]
        public decimal PointsPct { get; set; }

        [Column(TypeName = "decimal(3,1)")]
        [Display(Name = "GB")]
        public decimal DivisionGamesBehind { get; set; }

        [Column(TypeName = "decimal(3,1)")]
        [Display(Name = "GB")]
        public decimal ConferenceGamesBehind { get; set; }

        [Column(TypeName = "decimal(3,1)")]
        [Display(Name = "GB")]
        public decimal LeagueGamesBehind { get; set; }

        [Column(TypeName = "decimal(3,1)")]
        [Display(Name = "GB")]
        public decimal PlayoffsGamesBehind { get; set; }

        [Display(Name = "GF")]
        public int GoalsFor { get; set; }

        [Display(Name = "GA")]
        public int GoalsAgainst { get; set; }

        [Display(Name = "GD")]
        public int GoalDifferential { get; set; }

        [Display(Name = "GF-GA (GD)")]
        public string GoalRatio => $"{GoalsFor}-{GoalsAgainst} (" + (GoalDifferential > 0 ? "+" : "") + $"{GoalDifferential})";

        [Display(Name = "Streak")]
        public int Streak { get; set; }

        [Display(Name = "SGF")]
        public int StreakGoalsFor { get; set; }

        [Display(Name = "SGA")]
        public int StreakGoalsAgainst { get; set; }

        [Display(Name = "SGD")]
        public int StreakGoalDifferential { get; set; }

        [Display(Name = "Stk. GF-GA (GD)")]
        public string StreakGoalRatio => $"{StreakGoalsFor}-{StreakGoalsAgainst} (" + (StreakGoalDifferential > 0 ? "+" : "") + $"{StreakGoalDifferential})";

        public int GamesPlayedVsDivision { get; set; }
        
        public int WinsVsDivision { get; set; }
        
        public int LossesVsDivision { get; set; }
        
        public int OTLossesVsDivision { get; set; }

        [Display(Name = "vs. Div.")]
        public string RecordVsDivision_2021Format => $"{WinsVsDivision}-{LossesVsDivision}-{OTLossesVsDivision}";

        [Display(Name = "vs. Div.")]
        public string RecordVsDivision_2024Format => $"{WinsVsDivision}-{LossesVsDivision}";

        [Column(TypeName = "decimal(4,3)")]
        public decimal WinPctVsDivision { get; set; }

        public int GamesPlayedVsConference { get; set; }
        
        public int WinsVsConference { get; set; }

        public int OTWinsVsConference { get; set; }

        public int OTLossesVsConference { get; set; }

        public int LossesVsConference { get; set; }

        [Display(Name = "vs. Conf.")]
        public string RecordVsConference_2021Format => $"{WinsVsConference}-{LossesVsConference}-{OTLossesVsConference}";

        [Display(Name = "vs. Conf.")]
        public string RecordVsConference_2024Format => $"{WinsVsConference}-{LossesVsConference}";

        [Display(Name = "vs. Conf.")]
        public string RecordVsConference_2026Format => $"{WinsVsConference}-{OTWinsVsConference}-{OTLossesVsConference}-{LossesVsConference}";

        public int PointsVsConference { get; set; }

        [Column(TypeName = "decimal(4,3)")]
        public decimal WinPctVsConference { get; set; }

        public int InterConfGamesPlayed { get; set; }
        
        public int InterConfWins { get; set; }

        public int InterConfOTWins { get; set; }

        public int InterConfOTLosses { get; set; }

        public int InterConfLosses { get; set; }

        [Display(Name = "EC/WC")]
        public string InterConfRecord_2021Format => $"{InterConfWins}-{InterConfLosses}-{InterConfOTLosses}";

        [Display(Name = "EC/WC")]
        public string InterConfRecord_2024Format => $"{InterConfWins}-{InterConfLosses}";

        [Display(Name = "EC/WC")]
        public string InterConfRecord_2026Format => $"{InterConfWins}-{InterConfOTWins}-{InterConfOTLosses}-{InterConfLosses}";

        public int InterConfPoints { get; set; }

        [Column(TypeName = "decimal(4,3)")]
        public decimal InterConfWinPct { get; set; }

        [Display(Name = "GP_L10")]
        public int GamesPlayedInLast10Games { get; set; }

        [Display(Name = "W_L10")]
        public int WinsInLast10Games { get; set; }

        public int OTWinsInLast10Games { get; set; }

        public int OTLossesInLast10Games { get; set; }

        [Display(Name = "L_L10")]
        public int LossesInLast10Games { get; set; }

        public int PointsInLast10Games { get; set; }

        [Display(Name = "W%_L10")]
        [Column(TypeName = "decimal(4,3)")]
        public decimal WinPctInLast10Games { get; set; }

        [Display(Name = "Last 10")]
        public string RecordInLast10Games_2024Format => $"{WinsInLast10Games}-{LossesInLast10Games}";

        [Display(Name = "Last 10")]
        public string RecordInLast10Games_2026Format => $"{WinsInLast10Games}-{OTWinsInLast10Games}-{OTLossesInLast10Games}-{LossesInLast10Games} ({PointsInLast10Games})";



        public override string ToString()
        {
            return $"{Season.Year} - {Team}";
        }
    }
}
