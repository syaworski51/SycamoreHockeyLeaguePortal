using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SHLAPI.Models;

public partial class Schedule
{
    private const string REGULAR_SEASON = "Regular Season";

    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public DateTime Date { get; set; }

    public string Type { get; set; } = null!;

    public Guid AwayTeamId { get; set; }

    public int AwayScore { get; set; }

    public Guid HomeTeamId { get; set; }

    public int HomeScore { get; set; }

    public int Period { get; set; }

    public string LiveStatus { get; set; }

    /*public bool IsLive => LiveStatus == "Live";

    public bool IsFinalized => LiveStatus == "Finalized";*/

    public string? Notes { get; set; }

    public string Status
    {
        get
        {
            int ot;
            string suffix;

            if (LiveStatus != "Finalized")
            {
                if (Period == 0)
                    return "Not started";

                if (Period >= 4)
                {
                    if (Period > 4)
                    {
                        if (Type == REGULAR_SEASON)
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
                if (Period >= 4)
                {
                    if (Period > 4)
                    {
                        if (Type == REGULAR_SEASON)
                            return "Final - SO";

                        ot = Period - 3;
                        return $"Final - {ot}OT";
                    }

                    return "Final - OT";
                }

                return "Final";
            }
        }
    }

    private string GetOrdinalSuffix(int value)
    {
        // If the remainder of value/100 is outside the range of 11 and 13...
        if (value % 100 < 11 || value % 100 > 13)
        {
            // Get the remainder of value/10 and determine what to do next
            switch (value % 10)
            {
                case 1:  // value % 10 == 1 ? "1st"
                    return "st";

                case 2:  // value % 10 == 2 ? "2nd"
                    return "nd";

                case 3:  // value % 10 == 3 ? "3rd"
                    return "rd";
            }
        }

        // By default, return "th"
        return "th";
    }

    public int GameIndex { get; set; }

    public Guid? PlayoffRoundId { get; set; }

    public Guid? PlayoffSeriesId { get; set; }

    public virtual Team AwayTeam { get; set; } = null!;

    public virtual Team HomeTeam { get; set; } = null!;

    public virtual PlayoffRound? PlayoffRound { get; set; }

    public virtual PlayoffSeries? PlayoffSeries { get; set; }

    public virtual Season Season { get; set; } = null!;

    public int? PlayoffGameIndex { get; set; }

    public string? PlayoffSeriesScore { get; set; }
}
