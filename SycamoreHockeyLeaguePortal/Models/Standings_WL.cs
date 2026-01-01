using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class Standings_WL
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        public Season Season { get; set; }

        [ForeignKey(nameof(Conference))]
        public Guid? ConferenceId { get; set; }

        public Conference? Conference { get; set; }

        [ForeignKey(nameof(Division))]
        public Guid? DivisionId { get; set; }

        public Division? Division { get; set; }

        public int DivisionRanking { get; set; }

        public int ConferenceRanking { get; set; }

        public int PlayoffRanking { get; set; }

        public int LeagueRanking { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid TeamId { get; set; }

        [Display(Name = "Team")]
        public Team Team { get; set; }

        [Display(Name = "GP")]
        public int GamesPlayed { get; set; }

        [Display(Name = "W")]
        public int Wins { get; set; }

        [Display(Name = "L")]
        public int Losses { get; set; }

        [Display(Name = "W-L")]
        public string Record => $"{Wins}-{Losses}";

        [Column(TypeName = "decimal(4, 3)")]
        public decimal WinPct { get; set; }

        [Column(TypeName = "decimal(3, 1)")]
        public decimal DivisionGamesBehind { get; set; }

        [Column(TypeName = "decimal(3, 1)")]
        public decimal ConferenceGamesBehind { get; set; }

        [Column(TypeName = "decimal(3, 1)")]
        public decimal PlayoffGamesBehind { get; set; }

        [Column(TypeName = "decimal(3, 1)")]
        public decimal LeagueGamesBehind { get; set; }

        [Display(Name = "RW")]
        public int RegulationWins { get; set; }

        [Display(Name = "ROW")]
        public int RegPlusOTWins { get; set; }

        [Display(Name = "Streak")]
        public int Streak { get; set; }

        [Display(Name = "GF")]
        public int GoalsFor { get; set; }

        [Display(Name = "GA")]
        public int GoalsAgainst { get; set; }

        [Display(Name = "GD")]
        public int GoalDifferential { get; set; }

        public int DivisionGamesPlayed { get; set; }

        public int DivisionWins { get; set; }

        public int DivisionLosses { get; set; }

        public string DivisionRecord => $"{DivisionWins}-{DivisionLosses}";

        [Column(TypeName = "decimal(4, 3)")]
        public decimal DivisionWinPct { get; set; }

        public int ConferenceGamesPlayed { get; set; }

        public int ConferenceWins { get; set; }

        public int ConferenceLosses { get; set; }

        public string ConferenceRecord => $"{ConferenceWins}-{ConferenceLosses}";

        [Column(TypeName = "decimal(4, 3)")]
        public decimal ConferenceWinPct { get; set; }

        public int InterConfGamesPlayed { get; set; }

        public int InterConfWins { get; set; }

        public int InterConfLosses { get; set; }

        public string InterConfRecord => $"{InterConfWins}-{InterConfLosses}";

        [Column(TypeName = "decimal(4, 3)")]
        public decimal InterConfWinPct { get; set; }

        public int Last10GamesPlayed { get; set; }

        public int Last10Wins { get; set; }

        public int Last10Losses { get; set; }

        public string Last10Record => $"{Last10Wins}-{Last10Losses}";

        [Column(TypeName = "decimal(4, 3)")]
        public decimal Last10WinPct { get; set; }
    }
}
