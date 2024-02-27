using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("PlayoffStatuses")]
    public class PlayoffStatus
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public int Index { get; set; }
        public int ActiveFrom { get; set; }
        public int? ActiveTo { get; set; }
    }
}
