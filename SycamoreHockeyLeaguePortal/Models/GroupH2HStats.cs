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

        public GroupH2HStats()
        {
            Wins = 0;
            Losses = 0;
        }
    }
}
