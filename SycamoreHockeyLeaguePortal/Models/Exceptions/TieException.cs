namespace SycamoreHockeyLeaguePortal.Models.Exceptions
{
    public class TieException : Exception
    {
        private const string MESSAGE = "Games cannot end in a tie.";

        public TieException() : base(MESSAGE) { }

        public TieException(Exception inner) : base(MESSAGE, inner) { }
    }
}
