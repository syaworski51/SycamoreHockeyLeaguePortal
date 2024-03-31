using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class PlayoffStatus
{
    public Guid Id { get; set; }

    public string Symbol { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Index { get; set; }

    public int ActiveFrom { get; set; }

    public int? ActiveTo { get; set; }
}
