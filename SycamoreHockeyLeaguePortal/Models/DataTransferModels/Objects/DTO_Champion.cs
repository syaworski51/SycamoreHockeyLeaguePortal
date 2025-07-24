using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SycamoreHockeyLeaguePortal.Models.DataTransferModels.Objects
{
    public class DTO_Champion
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Guid TeamId { get; set; }
    }
}
