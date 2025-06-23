using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Teams")]
    public class Team
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Conference))]
        public Guid? ConferenceId { get; set; }
        
        public Conference? Conference { get; set; }

        [ForeignKey(nameof(Division))]
        public Guid? DivisionId { get; set; }

        public Division? Division { get; set; }
        
        public string Code { get; set; }
        
        public string City { get; set; }
        
        public string Name { get; set; }
        
        public string FullName => $"{City} {Name}";
        
        public string? AlternateName { get; set; }
        
        public string? LogoPath { get; set; }
        
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return FullName;
        }
    }
}
