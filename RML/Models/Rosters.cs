using Newtonsoft.Json;
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

        public List<string> PlayerNames { get; set; }

        public double TeamRankingAverage { get; set; }

        public double QBRankingAverage { get; set; }

        public double RBRankingAverage { get; set; }

        public double WRRankingAverage { get; set; }

        public double TERankingAverage { get; set; }


    }
}
