namespace SycamoreHockeyLeaguePortal.Models
{
    public class PlayerStats
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Season Season { get; set; }
        public Guid TeamId { get; set; }
        public Team Team { get; set; }
        public Guid PlayerId { get; set; }
        public Player Player { get; set; }
        public int GamesPlayed { get; set; }
    }
}
