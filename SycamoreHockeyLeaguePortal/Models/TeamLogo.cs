using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("TeamLogos")]
    public class TeamLogo
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid TeamId { get; set; }

        [Display(Name = "Team")]
        public Team Team { get; set; }

        [ForeignKey(nameof(ActiveFrom))]
        public Guid ActiveFromId { get; set; }

        [Display(Name = "Active From")]
        public Season ActiveFrom { get; set; }

        [ForeignKey(nameof(ActiveTo))]
        public Guid ActiveToId { get; set; }

        [Display(Name = "To")]
        public Season ActiveTo { get; set; }

        [Display(Name = "Path")]
        public string Path { get; set; }
    }
}
