using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class Season
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public int GamesPerTeam { get; set; }

    public virtual ICollection<Alignment> Alignments { get; set; } = new List<Alignment>();

    public virtual ICollection<Champion> Champions { get; set; } = new List<Champion>();

    public virtual ICollection<PlayoffRound> PlayoffRounds { get; set; } = new List<PlayoffRound>();

    public virtual ICollection<PlayoffSeries> PlayoffSeries { get; set; } = new List<PlayoffSeries>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Standings> Standings { get; set; } = new List<Standings>();
}
