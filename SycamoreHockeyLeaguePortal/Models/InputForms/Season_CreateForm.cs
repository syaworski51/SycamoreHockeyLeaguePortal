namespace SycamoreHockeyLeaguePortal.Models.InputForms
{
    public class Season_CreateForm
    {
        public int Year { get; set; }
        public int GamesPerTeam { get; set; }
        public Guid? EasternReplacementId { get; set; }
        public Team? EasternReplacement { get; set; }
        public Guid? WesternReplacementId { get; set; }
        public Team? WesternReplacement { get; set; }
    }
}
