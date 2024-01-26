﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SycamoreHockeyLeaguePortal.Models
{
    [Table("Seasons")]
    public class Season
    {
        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Long Name")]
        public string LongName => $"{Year} SHL Season";

        [Display(Name = "Short Name")]
        public string ShortName => $"SHL {Year}";

        [Display(Name = "Games Per Team")]
        public int GamesPerTeam { get; set; }
    }
}
