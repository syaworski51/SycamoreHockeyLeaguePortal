using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class ChampionsRound
{
    public Guid Id { get; set; }

    public Guid ChampionId { get; set; }

    public int RoundIndex { get; set; }

    public Guid OpponentId { get; set; }

    public int SeriesLength { get; set; }

    public int BestOf { get; set; }

    public virtual Champion Champion { get; set; } = null!;

    public virtual Team Opponent { get; set; } = null!;
}
