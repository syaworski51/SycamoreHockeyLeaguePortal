namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Packages
{
    public class DTP_NewChampion
    {
        public Champion Champion { get; set; }
        public List<ChampionsRound> Rounds { get; set; }
    }
}
