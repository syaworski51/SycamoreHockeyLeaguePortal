namespace SycamoreHockeyLeaguePortal.Models.InputForms
{
    public class Season_CreateForm
    {
        public int Year { get; set; }
        public int GamesPerTeam { get; set; }
        public List<Conference> Conferences { get; set; }
        public List<Division> Divisions { get; set; }
        public List<Alignment> Alignments { get; set; }
    }
}
