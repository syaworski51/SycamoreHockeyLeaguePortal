using Microsoft.CodeAnalysis.CSharp.Syntax;
using SycamoreHockeyLeaguePortal.Models;

namespace SycamoreHockeyLeaguePortal.MongoDBModels
{
    public class Standings
    {
        public Season Season { get; set; }
        public Conference Conference { get; set; }
        public Division Division { get; set; }
        public Team Team { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int OTLosses { get; set; }
        public int Points { get; set; }
        public decimal WinPct { get; set; }
        public decimal DivisionGamesBehind { get; set; }
        public decimal ConferenceGamesBehind { get; set; }
        public decimal LeagueGamesBehind { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifferential { get; set; }
        public int Streak { get; set; }
        public int GamesPlayedVsDivision { get; set; }
        public int WinsVsDivision { get; set; }
        public int LossesVsDivision { get; set; }
        public int OTLossesVsDivision { get; set; }
        public decimal WinPctVsDivision { get; set; }
        public int GamesPlayedVsConference { get; set; }
        public int WinsVsConference { get; set; }
        public int LossesVsConference { get; set; }
        public int OTLossesVsConference { get; set; }
        public decimal WinPctVsConference { get; set; }
        public int InterConfGamesPlayed { get; set; }
        public int InterConfWins { get; set; }
        public int InterConfLosses { get; set; }
        public int InterConfOTLosses { get; set; }
        public decimal InterConfWinPct { get; set; }
        public int WinsInLast10Games { get; set; }
        public int LossesInLast10Games { get; set; }
        public decimal WinPctInLast10Games { get; set; }
    }
}
