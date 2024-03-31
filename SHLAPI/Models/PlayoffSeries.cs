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
}
