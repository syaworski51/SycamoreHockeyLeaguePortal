using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class GameType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string ParameterValue { get; set; } = null!;

    public int Index { get; set; }
}
