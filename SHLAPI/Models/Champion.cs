using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class Champion
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public Guid TeamId { get; set; }

    public virtual ICollection<ChampionsRound> ChampionsRounds { get; set; } = new List<ChampionsRound>();

    public virtual Season Season { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
