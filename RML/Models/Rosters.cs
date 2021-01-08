﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RML.Models
{
    public class Rosters
    {
        [JsonProperty(PropertyName = "roster_id")]
        public string RosterID { get; set; }

        [JsonProperty(PropertyName = "starters")]
        public string[] Starters { get; set; }

        [JsonProperty(PropertyName = "players")]
        public string[] Bench { get; set; }

        [JsonProperty(PropertyName = "taxi")]
        public string[] TaxiSquad { get; set; }

        [JsonProperty(PropertyName = "owner_id")]
        public string OwnerID { get; set; }

        //[JsonProperty(PropertyName = "wins")]
        //public string Wins { get; set; }

        //[JsonProperty(PropertyName = "losses")]
        //public string Losses { get; set; }

        public string DisplayName { get; set; }

        public List<string> PlayerNames { get; set; }

        public List<string> PlayerTradeValues { get; set; }

        public double TeamRankingAverage { get; set; }
        public double QBRankingAverage { get; set; }
        public double RBRankingAverage { get; set; }
        public double WRRankingAverage { get; set; }
        public double TERankingAverage { get; set; }

        public int SelectedRoster { get; set; }

        public Dictionary<string, POR> PlayersOnRoster { get; set; }

        public int QBRanking { get; set; }
        public int RBRanking { get; set; }
        public int WRRanking { get; set; }
        public int TERanking { get; set; }
        public int TotalRanking { get; set; }


        public List<string> TradeCandidates { get; set; }

        public int TotalCandidateAdvantage { get; set; }
        public int TotalCandidateDisadvantage { get; set; }
        public int TotalDisparity { get; set; }
    }

    public class POR
    {
        public string PORName { get; set; }
        public string PORPosition { get; set; }
        public int PORValue { get; set; }
    }
}
