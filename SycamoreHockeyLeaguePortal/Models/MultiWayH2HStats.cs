namespace SycamoreHockeyLeaguePortal.Models
{
    public class MultiWayH2HStats
    {
        public int GamesPlayed => Wins + Losses;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinPct => 100 * ((decimal)Wins / GamesPlayed);

        public MultiWayH2HStats()
        {
            Wins = 0;
            Losses = 0;
        }
    }
}
