using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class StandingsSortOption
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Parameter { get; set; } = null!;

    public int Index { get; set; }

    public int FirstYear { get; set; }

    public int? LastYear { get; set; }
}
