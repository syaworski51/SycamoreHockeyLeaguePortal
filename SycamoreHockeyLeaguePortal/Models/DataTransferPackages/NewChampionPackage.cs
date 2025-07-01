namespace SycamoreHockeyLeaguePortal.Models.DataTransferPackages
{
    public class NewChampionPackage
    {
        public Champion Champion { get; set; }
        public List<ChampionsRound> Rounds { get; set; }
    }
}
