using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    /// <summary>
    ///     Represents a playoff round won by the champion of its season.
    /// </summary>
    [Table("Rounds")]
    public class ChampionsRound
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Champion))]
        public Guid ChampionId { get; set; }

        [Display(Name = "Champion")]
        public Team Champion { get; set; }

        [Display(Name = "Round")]
        public int RoundIndex { get; set; }

        [ForeignKey(nameof(Opponent))]
        public Guid OpponentId { get; set; }

        [Display(Name = "Opponent")]
        public Team Opponent { get; set; }

        [Display(Name = "Series Length")]
        public int SeriesLength { get; set; }

        [Display(Name = "Best Of")]
        public int BestOf { get; set; }
    }
}
