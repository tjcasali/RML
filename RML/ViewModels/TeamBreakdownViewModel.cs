using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RML.Models;

namespace RML.ViewModels
{
    public class TeamBreakdownViewModel
    {
        public List<Rosters> Rosters { get; set; }
        public string LeagueID { get; set; }

        public Rosters SelectedRosterVM { get; set; }
        public Rosters TCRoster { get; set; }
    }
}