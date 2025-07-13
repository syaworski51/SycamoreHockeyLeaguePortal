using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class RankedPlayoffSeries
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }
        
        public Season Season { get; set; }

        [ForeignKey(nameof(Round))]
        public Guid RoundId { get; set; }

        public PlayoffRound Round { get; set; }

        [ForeignKey(nameof(Matchup))]
        public Guid MatchupId { get; set; }
        
        public PlayoffSeries Matchup { get; set; }

        [Column(TypeName = "decimal(6, 3)")]
        public decimal FinalScore { get; set; }


        [Column(TypeName = "decimal(3, 1)")]
        public decimal SeriesCompetitivenessScore { get; set; }
        
        [Column(TypeName = "decimal(3, 1)")]
        public decimal OvertimeImpactScore { get; set; }
        
        [Column(TypeName = "decimal(3, 1)")]
        public decimal OverallGoalDiffScore { get; set; }
        
        [Column(TypeName = "decimal(3, 1)")]
        public decimal SeriesTiesScore { get; set; }


        public int SeasonRanking { get; set; }

        public int OverallRanking { get; set; }
    }
}
