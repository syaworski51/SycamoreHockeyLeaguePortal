using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Teams")]
    public class Team
    {
        [Key]
        public Guid Id { get; set; }

        
    }
}
