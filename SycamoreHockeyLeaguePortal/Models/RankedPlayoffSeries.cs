namespace SycamoreHockeyLeaguePortal.Models
{
    public class RankedPlayoffSeries
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Season Season { get; set; }
        public Guid RoundId { get; set; }
        public PlayoffRound Round { get; set; }
        public Guid MatchupId { get; set; }
        public PlayoffSeries Matchup { get; set; }
        
        public byte Length { get; set; }
        
        public decimal Game1OTPoints { get; set; }
        public decimal Game2OTPoints { get; set; }
        public decimal Game3OTPoints { get; set; }
        public decimal Game4OTPoints { get; set; }
        public decimal Game5OTPoints { get; set; }
        public decimal Game6OTPoints { get; set; }
        public decimal Game7OTPoints { get; set; }
        public decimal TotalOTPoints { get; set; }
        
        public byte Game1MarginPoints { get; set; }
        public byte Game2MarginPoints { get; set; }
        public byte Game3MarginPoints { get; set; }
        public byte Game4MarginPoints { get; set; }
        public byte Game5MarginPoints { get; set; }
        public byte Game6MarginPoints { get; set; }
        public byte Game7MarginPoints { get; set; }
        public byte TotalGameMarginPoints { get; set; }

        public decimal GoalDifferentialPoints { get; set; }

        public byte SeriesTiesPoints { get; set; }

        public decimal OverallScore { get; set; }
    }
}
