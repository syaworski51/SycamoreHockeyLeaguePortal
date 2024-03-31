using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class Alignment
{
    public Guid Id { get; set; }

    public Guid? ConferenceId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid TeamId { get; set; }

    public Guid SeasonId { get; set; }

    public virtual Conference? Conference { get; set; }

    public virtual Division? Division { get; set; }

    public virtual Season Season { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
