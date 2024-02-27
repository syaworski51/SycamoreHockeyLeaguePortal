using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("GameTypes")]
    public class GameType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ParameterValue { get; set; }
        public int Index { get; set; }
    }
}
