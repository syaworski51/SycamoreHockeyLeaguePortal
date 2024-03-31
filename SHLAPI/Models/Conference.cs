using System;
using System.Collections.Generic;

namespace SHLAPI.Models;

public partial class Conference
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Alignment> Alignments { get; set; } = new List<Alignment>();

    public virtual ICollection<Standings> Standings { get; set; } = new List<Standings>();
}
