using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("ProgramFlags")]
    public class ProgramFlag
    {
        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "State")]
        public bool State { get; set; }
    }
}
