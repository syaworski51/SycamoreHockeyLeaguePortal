using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("TeamBrandingHistory")]
    public class TeamBrandingHistory
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Season Season { get; set; }
        public Guid TeamId { get; set; }
        public Team Team { get; set; }
        public string City { get; set; }
        public string Name { get; set; }
        public string AlternateName { get; set; }
        public string FullName => $"{City} {Name}";
        public string LogoPath { get; set; }
    }
}
