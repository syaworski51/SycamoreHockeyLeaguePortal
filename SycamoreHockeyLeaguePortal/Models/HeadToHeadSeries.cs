namespace SycamoreHockeyLeaguePortal.Models
{
    public class HeadToHeadSeries
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Season Season { get; set; }
        public Guid Team1Id { get; set; }
        public Team Team1 { get; set; }
        public int Team1Wins { get; set; }
        public int Team1OTWins { get; set; }
        public int Team1ROWs { get; set; }
        public int Team1Points { get; set; }
        public int Team1GoalsFor { get; set; }
        public Guid Team2Id { get; set; }
        public Team Team2 { get; set; }
        public int Team2Wins { get; set; }
        public int Team2OTWins { get; set; }
        public int Team2ROWs { get; set; }
        public int Team2Points { get; set; }
        public int Team2GoalsFor { get; set; }
    }
}
