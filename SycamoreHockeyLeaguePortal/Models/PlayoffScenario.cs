using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class PlayoffScenario
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        [Display(Name = "Season")]
        public Season Season { get; set; }

        [ForeignKey(nameof(Team))]
        public Guid TeamId { get; set; }

        [Display(Name = "Team")]
        public Team Team { get; set; }
        
        // "Demotion", "Elimination", "Clinching", "Home-ice", "Division", "Conference", "President's Trophy"
        public string Type { get; set; }
        
        public string Description { get; set; }

        public DateTime EvaluationDate { get; set; }
        
        // List of conditions in JSON format
        public string Conditions { get; set; }
        
        // "TBD", "Yes", "No"
        public string ConditionsMet { get; set; }
    }
}
