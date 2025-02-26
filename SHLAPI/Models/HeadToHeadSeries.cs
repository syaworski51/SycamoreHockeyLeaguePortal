namespace SHLAPI.Models
{
    public class HeadToHeadSeries
    {
        public Guid Id { get; set; }
        public Guid SeasonId { get; set; }
        public Season Season { get; set; }
        public Guid Team1Id { get; set; }
        public Team Team1 { get; set; }
        public int Team1Wins { get; set; }
        public Guid Team2Id { get; set; }
        public Team Team2 { get; set; }
        public int Team2Wins { get; set; }
    }
}
