using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RML.Models;

namespace RML.ViewModels
{
    public class DisplayLeagueViewModel
    {
        public List<Rosters> Rosters { get; set; }
        //public List<Players> Players { get; set; }

        public Players Players { get; set; }
    }
}