using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RML.Models
{
    public class PlayerRanking
    {
        public string PlayerName { get; set; }
        public string OverallRanking { get; set; }
        public string PositionalRanking { get; set; }

        public PlayerRanking(string pn, string or, string pr)
        {
            PlayerName = pn;
            OverallRanking = or;
            PositionalRanking = pr;
        }
    }

}
