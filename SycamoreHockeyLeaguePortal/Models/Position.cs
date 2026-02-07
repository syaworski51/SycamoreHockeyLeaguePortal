using System.ComponentModel.DataAnnotations;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class Position
    {
        [Key]
        public string Symbol { get; set; }
        
        public string Name { get; set; }
    }
}
