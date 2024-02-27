using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Teams")]
    public class Team
    {
        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Code")]
        public string Code { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Full Name")]
        public string FullName => $"{City} {Name}";

        public string? AlternateName { get; set; }

        [Display(Name = "Logo Path")]
        public string? LogoPath { get; set; }

        [Display(Name = "Primary Color")]
        public string PrimaryColor { get; set; }

        [Display(Name = "Secondary Color")]
        public string? SecondaryColor { get; set; }

        [Display(Name = "Tertiary Color")]
        public string? TertiaryColor { get; set; }
    }
}
