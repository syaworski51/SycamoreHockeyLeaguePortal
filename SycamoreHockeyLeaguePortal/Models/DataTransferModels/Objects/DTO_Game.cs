using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_Game
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public DateTime Date { get; set; }
        public int GameIndex { get; set; }
        public string Type { get; set; }
        public Guid? PlayoffRoundId { get; set; }
        public Guid? PlayoffSeriesId { get; set; }
        public int? PlayoffGameIndex { get; set; }
        public string? PlayoffSeriesScore { get; set; }
        public Guid AwayTeamId { get; set; }
        public int AwayScore { get; set; }
        public Guid HomeTeamId { get; set; }
        public int HomeScore { get; set; }
        public int Period { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsLive { get; set; }
        public bool IsFinalized { get; set; }
        public string? Notes { get; set; }
    }
}
