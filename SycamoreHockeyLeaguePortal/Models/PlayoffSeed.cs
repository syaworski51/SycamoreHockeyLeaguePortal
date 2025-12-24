namespace SycamoreHockeyLeaguePortal.Models
{
    public class PlayoffSeed
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Season Season { get; set; }
        public Guid ConferenceId { get; set; }
        public Conference Conference { get; set; }
        public Guid? DivisionId { get; set; }
        public Division? Division { get; set; }
        public int Number { get; set; }
        public Guid TeamId { get; set; }
        public Team Team { get; set; }
    }
}
