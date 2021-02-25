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
        public string LeagueID { get; set; }
        public UserInfo UserInfo { get; set; }
        public string LastScrapeDate { get; set; }
        public Dictionary<string, string> DraftPickRankings { get; set; }

        public List<TradedPick> TradedPicks { get; set; }
    }
}