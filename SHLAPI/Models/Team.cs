using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class Team
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? LogoPath { get; set; }

    public string? AlternateName { get; set; }

    public virtual ICollection<Alignment> Alignments { get; set; } = new List<Alignment>();

    public virtual ICollection<Champion> Champions { get; set; } = new List<Champion>();

    public virtual ICollection<ChampionsRound> ChampionsRounds { get; set; } = new List<ChampionsRound>();

    public virtual ICollection<PlayoffSeries> PlayoffSeriesTeam1s { get; set; } = new List<PlayoffSeries>();

    public virtual ICollection<PlayoffSeries> PlayoffSeriesTeam2s { get; set; } = new List<PlayoffSeries>();

    public virtual ICollection<Schedule> ScheduleAwayTeams { get; set; } = new List<Schedule>();

    public virtual ICollection<Schedule> ScheduleHomeTeams { get; set; } = new List<Schedule>();

    public virtual ICollection<Standings> Standings { get; set; } = new List<Standings>();
}
