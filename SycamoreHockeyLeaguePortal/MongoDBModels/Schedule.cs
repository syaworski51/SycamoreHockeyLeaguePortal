using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.MongoDBModels
{
    public class Schedule
    {
        public Season Season { get; set; }
        public DateTime Date { get; set; }
        public int GameIndex { get; set; }
        public string Type { get; set; }
        public PlayoffRound? PlayoffRound { get; set; }
        public int? PlayoffGameIndex { get; set; }
        public string? PlayoffSeriesScore { get; set; }
        public Team AwayTeam { get; set; }
        public int AwayScore { get; set; }
        public Team HomeTeam { get; set; }
        public int HomeScore { get; set; }
        public int Period { get; set; }
        public bool IsLive { get; set; }
        public bool IsFinalized { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; }
    }
}
