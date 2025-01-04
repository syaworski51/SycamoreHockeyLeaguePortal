namespace SycamoreHockeyLeaguePortal.Models
{
    public class GroupH2HStats
    {
        public int GamesPlayed => Wins + Losses;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinPct => (GamesPlayed > 0) ? 
            100 * ((decimal)Wins / GamesPlayed) :
            0;
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifferential => GoalsFor - GoalsAgainst;
    }
}
