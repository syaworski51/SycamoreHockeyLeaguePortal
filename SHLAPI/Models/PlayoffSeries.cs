using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class PlayoffSeries
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public Guid RoundId { get; set; }

    public DateTime StartDate { get; set; }

    public Guid Team1Id { get; set; }

    public int Team1Wins { get; set; }

    public Guid Team2Id { get; set; }

    public int Team2Wins { get; set; }

    public virtual PlayoffRound Round { get; set; } = null!;

    public virtual Season Season { get; set; } = null!;

    public virtual Team Team1 { get; set; } = null!;

    public virtual Team Team2 { get; set; } = null!;

    public string SeriesScoreString
    {
        get
        {
            if (Team1Wins != Team2Wins)
            {
                string team1Name = Team1.AlternateName == null ? Team1.City : Team1.AlternateName;
                string team2Name = Team2.AlternateName == null ? Team2.City : Team2.AlternateName;
                string leader = (Team1Wins > Team2Wins) ? team1Name : team2Name;
                string verb = (Status == "In progress") ? "leads" : "wins";
                int leadingWins = Math.Max(Team1Wins, Team2Wins);
                int trailingWins = Math.Min(Team1Wins, Team2Wins);

                return $"{leader} {verb} series {leadingWins}-{trailingWins}";
            }

            return $"Series is tied {Team1Wins}-{Team2Wins}";
        }
    }

    public string ShortSeriesScoreString
    {
        get
        {
            if (Team1Wins != Team2Wins)
            {
                string team1Code = Team1.Code;
                string team2Code = Team2.Code;
                string leader = (Team1Wins > Team2Wins) ? team1Code : team2Code;
                string verb = (Status == "In progress") ? "leads" : "wins";
                int leadingWins = Math.Max(Team1Wins, Team2Wins);
                int trailingWins = Math.Min(Team1Wins, Team2Wins);

                return $"{leader} {verb} {leadingWins}-{trailingWins}";
            }

            return $"Series tied {Team1Wins}-{Team2Wins}";
        }
    }

    public string Status
    {
        get
        {
            if (Team1Wins == 4 || Team2Wins == 4)
                return "Series over";

            if (Team1Wins > 0 || Team2Wins > 0)
                return "In progress";

            return "Not started";
        }
    }
}
