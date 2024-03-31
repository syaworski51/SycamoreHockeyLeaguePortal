using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class ProgramFlag
{
    public Guid Id { get; set; }

    public string Description { get; set; } = null!;

    public bool State { get; set; }
}
