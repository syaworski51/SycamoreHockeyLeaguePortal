namespace SycamoreHockeyLeaguePortal.Models.Exceptions
{
    public class InvalidLossResultException : Exception
    {
        private const string MESSAGE = "Losing results cannot have more goals for than goals against";

        public InvalidLossResultException() : base(MESSAGE) { }

        public InvalidLossResultException(Exception inner) : base(MESSAGE, inner) { }
    }
}
