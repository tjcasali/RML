using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;


namespace RML.Models
{
    public class TradedPick
    {
        [JsonProperty(PropertyName = "season")]
        public string Season { get; set; }

        [JsonProperty(PropertyName = "round")]
        public int Round { get; set; }

        [JsonProperty(PropertyName = "roster_id")]
        public int RosterID { get; set; }

        [JsonProperty(PropertyName = "previous_owner_id")]
        public int PreviousOwnerID { get; set; }

        [JsonProperty(PropertyName = "owner_id")]
        public int OwnerID { get; set; }

        public string Position {get; set;}
    }
}