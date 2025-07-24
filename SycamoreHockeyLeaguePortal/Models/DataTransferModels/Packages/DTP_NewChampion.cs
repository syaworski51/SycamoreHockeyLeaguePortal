using SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects;

namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Packages
{
    public class DTP_NewChampion
    {
        public DTO_Champion Champion { get; set; }
        public List<DTO_ChampionsRound> Rounds { get; set; }

        public DTP_NewChampion(DTO_Champion champion)
        {
            Champion = champion;
            Rounds = new();
        }
    }
}
