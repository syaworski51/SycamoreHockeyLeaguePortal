using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_RankedPlayoffSeries
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Guid RoundId { get; set; }
        public Guid MatchupId { get; set; }
        public decimal FinalScore { get; set; }
        
        public decimal SeriesCompetitivenessScore { get; set; }
        public decimal OvertimeImpactScore { get; set; }
        public decimal OverallGoalDiffScore { get; set; }
        public decimal SeriesTiesScore { get; set; }

        public int SeasonRanking { get; set; }
        public int OverallRanking { get; set; }
    }
}
