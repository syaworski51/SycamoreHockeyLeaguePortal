namespace SycamoreHockeyLeaguePortal.Models.Exceptions
{
    public class InvalidWinResultException : Exception
    {
        private const string MESSAGE = "Winning results cannot have fewer goals for than goals against.";
        
        public InvalidWinResultException() : base(MESSAGE) { }

        public InvalidWinResultException(Exception inner) : base(MESSAGE, inner) { }
    }
}
