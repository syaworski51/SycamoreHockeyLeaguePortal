namespace SycamoreHockeyLeaguePortal.Models.Exceptions
{
    public class SnapshotNotNeededException : Exception
    {
        public DateTime Date { get; set; }


        public SnapshotNotNeededException() { }

        public SnapshotNotNeededException(string message) : base(message) { }

        public SnapshotNotNeededException(string message, Exception inner) : base(message, inner) { }

        public SnapshotNotNeededException(string message, DateTime date) : this(message)
        {
            Date = date;
        }
    }
}
