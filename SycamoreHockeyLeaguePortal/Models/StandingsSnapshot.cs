using MongoDB.Bson;

namespace SycamoreHockeyLeaguePortal.Models
{
    public class StandingsSnapshot
    {
        public ObjectId Id { get; set; }
        public int Season { get; set; }
        public DateTime Date { get; set; }
        public List<Standings> Standings { get; set; }
    }
}
