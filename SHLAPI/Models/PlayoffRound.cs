using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class PlayoffRound
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public int Index { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<PlayoffSeries> PlayoffSeries { get; set; } = new List<PlayoffSeries>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual Season Season { get; set; } = null!;
}
