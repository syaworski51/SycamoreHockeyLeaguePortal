using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class Schedule
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public DateTime Date { get; set; }

    public string Type { get; set; } = null!;

    public Guid AwayTeamId { get; set; }

    public int? AwayScore { get; set; }

    public Guid HomeTeamId { get; set; }

    public int? HomeScore { get; set; }

    public int Period { get; set; }

    public bool IsLive { get; set; }

    public string? Notes { get; set; }

    public int GameIndex { get; set; }

    public Guid? PlayoffRoundId { get; set; }

    public bool? IsFinalized { get; set; }

    public virtual Team AwayTeam { get; set; } = null!;

    public virtual Team HomeTeam { get; set; } = null!;

    public virtual PlayoffRound? PlayoffRound { get; set; }

    public virtual Season Season { get; set; } = null!;
}
