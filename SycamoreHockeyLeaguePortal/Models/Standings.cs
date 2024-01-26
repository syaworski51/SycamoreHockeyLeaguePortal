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

        [Display(Name = "L")]
        public int Losses { get; set; }

        [Display(Name = "OTL")]
        public int OTLosses { get; set; }

        [Display(Name = "Record")]
        public string Record_2021Format => $"{Wins}-{Losses}-{OTLosses}";
        
        [Display(Name = "Record")]
        public string Record_2024Format => $"{Wins}-{Losses}";

        [Display(Name = "RW")]
        public int RegulationWins { get; set; }

        [Display(Name = "ROW")]
        public int RegPlusOTWins { get; set; }

        [Display(Name = "Pts.")]
        public int Points { get; set; }

        [Display(Name = "MPP")]
        public int MaximumPossiblePoints { get; set; }

        [Column(TypeName = "decimal(4,1)")]
        [Display(Name = "Win %")]
        public decimal WinPct { get; set; }

        [Column(TypeName = "decimal(4,1)")]
        [Display(Name = "Pts. %")]
        public decimal PointsPct { get; set; }

        [Column(TypeName = "decimal(2,1)")]
        [Display(Name = "GB")]
        public decimal GamesBehind { get; set; }

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

        public int GamesPlayedVsDivision { get; set; }
        
        public int WinsVsDivision { get; set; }
        
        public int LossesVsDivision { get; set; }
        
        public int OTLossesVsDivision { get; set; }

        [Display(Name = "vs. Division")]
        public string RecordVsDivision_2021Format => $"{WinsVsDivision}-{LossesVsDivision}-{OTLossesVsDivision}";

        [Display(Name = "vs. Division")]
        public string RecordVsDivision_2024Format => $"{WinsVsDivision}-{LossesVsDivision}";

        [Column(TypeName = "decimal(4,1)")]
        public decimal WinPctVsDivision { get; set; }

        public int GamesPlayedVsConference { get; set; }
        
        public int WinsVsConference { get; set; }
        
        public int LossesVsConference { get; set; }
        
        public int OTLossesVsConference { get; set; }

        [Display(Name = "vs. Conference")]
        public string RecordVsConference_2021Format => $"{WinsVsConference}-{LossesVsConference}-{OTLossesVsConference}";

        [Display(Name = "vs. Conference")]
        public string RecordVsConference_2024Format => $"{WinsVsConference}-{LossesVsConference}";

        [Column(TypeName = "decimal(4,1)")]
        public decimal WinPctVsConference { get; set; }

        public int InterConfGamesPlayed { get; set; }
        
        public int InterConfWins { get; set; }
        
        public int InterConfLosses { get; set; }
        
        public int InterConfOTLosses { get; set; }

        [Display(Name = "Inter-Division")]
        public string InterConfRecord_2021Format => $"{InterConfWins}-{InterConfLosses}-{InterConfOTLosses}";

        [Display(Name = "Inter-Conf")]
        public string InterConfRecord_2024Format => $"{InterConfWins}-{InterConfLosses}";

        [Column(TypeName = "decimal(4,1)")]
        public decimal InterConfWinPct { get; set; }
    }
}
