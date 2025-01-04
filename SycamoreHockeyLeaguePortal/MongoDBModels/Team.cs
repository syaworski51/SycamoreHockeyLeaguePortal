using System.ComponentModel.DataAnnotations;

namespace SycamoreHockeyLeaguePortal.MongoDBModels
{
    public class Team
    {
        public string Code { get; set; }
        public string City { get; set; }
        public string Name { get; set; }
        public string? AlternateName { get; set; }
        public string? LogoPath { get; set; }

        public override string ToString()
        {
            return $"{City} {Name}";
        }
    }
}
