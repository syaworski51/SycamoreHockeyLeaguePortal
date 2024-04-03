using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Schedule")]
    public class Schedule
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Season))]
        public Guid SeasonId { get; set; }

        [Display(Name = "Season")]
        public Season Season { get; set; }

        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Display(Name = "Index")]
        public int GameIndex { get; set; }

        [Display(Name = "Type")]
        public string Type { get; set; }

        [ForeignKey(nameof(PlayoffRound))]
        public Guid? PlayoffRoundId { get; set; }

        [Display(Name = "Round")]
        public PlayoffRound? PlayoffRound { get; set; }

        [ForeignKey(nameof(AwayTeam))]
        public Guid AwayTeamId { get; set; }

        [Display(Name = "Away Team")]
        public Team AwayTeam { get; set; }

        [Display(Name = "Away Score")]
        public int AwayScore { get; set; }

        [ForeignKey(nameof(HomeTeam))]
        public Guid HomeTeamId { get; set; }

        [Display(Name = "Home Team")]
        public Team HomeTeam { get; set; }

        [Display(Name = "Home Score")]
        public int HomeScore { get; set; }

        [Display(Name = "Period")]
        public int Period { get; set; }

        [Display(Name = "Live?")]
        public bool IsLive { get; set; }

        [Display(Name = "Finalized?")]
        public bool IsFinalized { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Status")]
        public string Status
        {
            get
            {
                int ot;
                string suffix;
                
                if (IsLive)
                {
                    if (Period == 0)
                        return "Not started";

                    if (Period >= 4)
                    {
                        if (Period > 4)
                        {
                            if (Type == "Regular Season")
                                return "Shootout";

                            ot = Period - 3;
                            suffix = GetOrdinalSuffix(ot);
                            return $"{ot}{suffix} Overtime";
                        }

                        return "Overtime";
                    }

                    suffix = GetOrdinalSuffix(Period);
                    return $"{Period}{suffix} Period";
                }
                else
                {
                    if (Period >= 3)
                    {
                        if (Period >= 4)
                        {
                            if (Period > 4)
                            {
                                if (Type == "Regular Season")
                                    return "Final - SO";

                                ot = Period - 3;
                                return $"Final - {ot}OT";
                            }

                            return "Final - OT";
                        }

                        return "Final";
                    }

                    return "Not started";
                }
            }
        }



        /// <summary>
        ///     Start the game.
        /// </summary>
        public void StartGame()
        {
            IsLive = true;
            AwayScore = 0;
            HomeScore = 0;
            Period = 1;
        }

        /// <summary>
        ///     End the game.
        /// </summary>
        public void EndGame()
        {
            IsLive = false;
        }
        
        /// <summary>
        ///     Change the value of a property.
        /// </summary>
        /// <param name="value">The property to change (score, period).</param>
        /// <param name="change">The amount to change the value by.</param>
        private void ChangeValue(int value, int change)
        {
            if (IsLive)
                value += change;
        }

        /// <summary>
        ///     Advance to the next period.
        /// </summary>
        public void NextPeriod()
        {
            ChangeValue(Period, 1);
        }

        /// <summary>
        ///     Go back to the previous period.
        /// </summary>
        public void PreviousPeriod()
        {
            ChangeValue(Period, -1);
        }

        /// <summary>
        ///     Run when a goal is scored.
        /// </summary>
        /// <param name="team">The score of the team that scored the goal.</param>
        private void Goal(int team)
        {
            ChangeValue(team, 1);
        }

        /// <summary>
        ///     Run when the away team scores a goal.
        /// </summary>
        public void AwayGoal()
        {
            Goal((int)AwayScore);
        }

        /// <summary>
        ///     Run when the home team scores a goal.
        /// </summary>
        public void HomeGoal()
        {
            Goal((int)HomeScore);
        }

        /// <summary>
        ///     Run when a team has a goal taken away from them.
        /// </summary>
        /// <param name="team">The team that is having a goal taken away from them.</param>
        private void RemoveGoal(int team)
        {
            ChangeValue(team, -1);
        }

        /// <summary>
        ///     Run when the away team has a goal taken away from them.
        /// </summary>
        public void RemoveAwayGoal()
        {
            RemoveGoal((int)AwayScore);
        }

        /// <summary>
        ///     Run when the home team has a goal taken away from them.
        /// </summary>
        public void RemoveHomeGoal()
        {
            RemoveGoal((int)HomeScore);
        }

        /// <summary>
        ///     Get the ordinal suffix of an integer value (-st, -nd, -rd, -th).
        /// </summary>
        /// <param name="value">The value to get the ordinal suffix of.</param>
        /// <returns>The ordinal suffix of the value provided.</returns>
        private string GetOrdinalSuffix(int value)
        {
            // If value % 100 is outside of 11 and 13...
            if (value % 100 < 11 || value % 100 > 13)
            {
                switch (value % 10)
                {
                    case 1:
                        return "st";

                    case 2:
                        return "nd";

                    case 3:
                        return "rd";
                }
            }

            // By default, return "th"
            return "th";
        }
    }
}
