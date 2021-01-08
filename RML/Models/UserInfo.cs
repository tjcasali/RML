using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RML.Models
{
    public class UserInfo
    {
        public string LeagueID { get; set; }

        [JsonProperty(PropertyName = "roster_positions")]
        public string[] LeagueRosterPositions { get; set; }

        public bool SuperFlex { get; set; }
        //public string UserID { get; set; }
    }
}
