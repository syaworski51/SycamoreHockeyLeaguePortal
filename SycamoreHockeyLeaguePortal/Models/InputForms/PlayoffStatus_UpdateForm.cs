namespace SycamoreHockeyLeaguePortal.Models.InputForms
{
    public class PlayoffStatus_UpdateForm
    {
        public int Season { get; set; }
        public Team Team { get; set; }
        public string Status { get; set; }
        public string ViewBy { get; set; }
    }
}
