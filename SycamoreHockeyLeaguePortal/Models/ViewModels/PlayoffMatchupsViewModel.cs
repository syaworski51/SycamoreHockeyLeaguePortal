namespace SycamoreHockeyLeaguePortal.Models.ViewModels
{
    public class PlayoffMatchupsViewModel
    {
        public int Season { get; set; }
        public List<List<int[]>> Seeds { get; set; }
        public List<List<Standings[]>> Teams { get; set; }
        public List<Conference> Conferences { get; set; }
    }
}
