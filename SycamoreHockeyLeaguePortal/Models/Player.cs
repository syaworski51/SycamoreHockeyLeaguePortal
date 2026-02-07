using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class Player
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid? TeamId { get; set; }
        
        public Team? Team { get; set; }
        
        public int Number { get; set; }
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }

        [ForeignKey(nameof(Position))]
        public string PositionSymbol { get; set; }

        public Position Position { get; set; }
    }
}
