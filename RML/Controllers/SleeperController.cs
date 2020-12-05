using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RML.Models;
using RML.ViewModels;
using System.Net;
using RML.Controllers;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;

namespace RML.Controllers
{
    public class SleeperController : Controller
    {

        public List<SleeperUsers> sleeperUsers = new List<SleeperUsers>();
        public List<Rosters> sleeperRosters = new List<Rosters>();
        public Dictionary<string, PlayerData> playerList = new Dictionary<string, PlayerData>();


        // GET: Sleeper
        public ActionResult Index()
        {
            return View();
        }
        public async Task<ActionResult> DisplayLeague(UserInfo user)
        {
            sleeperUsers = await GetUsers(user);
            sleeperRosters = await GetRosters(user);
            playerList = await GetPlayers();

            LinkUsersAndRosters(sleeperUsers, sleeperRosters);

            AddPlayerNamesToRosters(sleeperRosters, playerList);

            LoadRankings(playerList);

            sleeperRosters = AverageTeamRanking(sleeperRosters, playerList);

            foreach(var p in playerList)
            {
                System.Diagnostics.Debug.WriteLine(p.Value.FirstName + " " + p.Value.LastName + p.Value.TradeValueChart);
            }

            var viewModel = new RostersViewModel
            {
                Rosters = sleeperRosters
            };

            return View(viewModel);
        }

        public async Task<List<Rosters>> GetRosters(UserInfo user)
        {

            HttpClient client = new HttpClient();

            //My roster ID: 325829344368812032
            //string leagueID = "515558249940426752";
            string leagueID = user.LeagueID;


            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID + "/rosters");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<Rosters> rosters = JsonConvert.DeserializeObject<List<Rosters>>(responseBody);

            return rosters;
        }

        /// Get Users
        /// Take in user submitted league ID and put that into the Sleeper API to return the Users in the league
        public async Task<List<SleeperUsers>> GetUsers(UserInfo user)
        {
            HttpClient client = new HttpClient();

            //string leagueID = "515558249940426752";
            string leagueID = user.LeagueID;

            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID + "/users");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<SleeperUsers> userList = JsonConvert.DeserializeObject<List<SleeperUsers>>(responseBody);

            return userList;

        }

        public async Task<Dictionary<string, PlayerData>> GetPlayers()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await
            client.GetAsync("https://api.sleeper.app/v1/players/nfl");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            Dictionary<string, PlayerData> playerList = JsonConvert.DeserializeObject<Dictionary<string, PlayerData>>(responseBody);


            return playerList;
        }

        public void LinkUsersAndRosters(List<SleeperUsers> users, List<Rosters> rosters)
        {
            foreach (Rosters ros in rosters)
            {
                foreach (SleeperUsers su in users)
                {
                    if (su.UserID == ros.OwnerID)
                    {
                        su.RosterID = ros.RosterID;
                        System.Diagnostics.Debug.WriteLine("Roster ID: " + su.RosterID + " Username: " + su.DisplayName);
                    }
                }
            }
        }

        public void AddPlayerNamesToRosters(List<Rosters> rosters, Dictionary<string, PlayerData> players)
        {
            List<string> tempPlayersList = new List<string>();
            string temp = "";

            foreach (Rosters ros in rosters)
            {
                tempPlayersList = new List<string>();
                foreach (string p in ros.Bench)
                {

                    temp = players[p].FirstName + " " + players[p].LastName;
                    tempPlayersList.Add(temp);
                }
                ros.PlayerNames = tempPlayersList;
            }
        }

        public void ScrapeRankings()
        {
            //System.Diagnostics.Debug.WriteLine("Testing2..");
            //string url = "https://www.fantasypros.com/nfl/rankings/dynasty-overall.php";

            //HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
            //HtmlAgilityPack.HtmlDocument doc = web.Load(url);

            //var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class,'table table-striped player-table table-hover js-table-caption')]");
            //var rankings = doc.DocumentNode.SelectNodes("//*[@*[contains(., 'mpb-player-')]]");
            //var table = doc.DocumentNode.SelectSingleNode("//table[@id='ranking-table']//tbody//tr");
            //var table = doc.DocumentNode.SelectSingleNode("//table[@id='ranking-table']");

            //System.Diagnostics.Debug.WriteLine(table.InnerHtml);
            //System.Diagnostics.Debug.WriteLine(table.InnerText);

            //foreach (HtmlNode col in doc.DocumentNode.SelectNodes("//table"))
            //{
            //    System.Diagnostics.Debug.WriteLine(col.InnerHtml);
            //}

            //string tempTest = "";
            //string tempOverallRank = "";
            //string tempPlayerName = "";
            //string tempPositionRank = "";
            //int counter = 0;

            //		item.InnerText	"4\nEzekiel ElliottE. Elliott DAL \n \nRB3\n10\n243\n8\n5.2\n0.9\n"	string


            //foreach (var row in table)
            //{

            //    //System.Diagnostics.Debug.WriteLine(row.InnerText);

            //    //foreach(var col in row.SelectNodes("td"))
            //    foreach (var col in row.InnerHtml)
            //    {
            //        tempOverallRank = row.SelectSingleNode("//td").InnerText;
            //        System.Diagnostics.Debug.WriteLine("I am here: " + tempOverallRank);

            //        //tempTest = (col.InnerText);
            //        ////tempPlayerName = row.SelectSingleNode("//td").InnerText;
            //        //System.Diagnostics.Debug.WriteLine(counter + " " + tempTest);
            //        //counter++;
            //    }
            //    //System.Diagnostics.Debug.WriteLine(row.InnerText);
            //}
        }

        public void LoadRankings(Dictionary<string, PlayerData> players)
        {
            using (var reader = new StreamReader("C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\Value_Scores_Full_Data_data.csv"))
            {
                List<string> playerNameList = new List<string>();
                List<string> playerPositionList = new List<string>();
                List<string> playerTradeValueList = new List<string>();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    playerNameList.Add(values[0]);
                    playerPositionList.Add(values[1]);
                    playerTradeValueList.Add(values[9]);
                }

                foreach (var p in players)
                {
                    string temp = "";
                    string firstNameTemp = p.Value.FirstName.Replace(".", string.Empty);
                    string lastNameTemp = p.Value.LastName.Replace(".", string.Empty);
                    temp = '"' + firstNameTemp + " " + lastNameTemp + '"';
                    temp = temp.Remove(0, 1);
                    temp = temp.Remove(temp.Length - 1, 1);
                    if (playerNameList.Contains(temp))
                    {
                        int listIndex = playerNameList.IndexOf(temp);
                        //string removeQuoteFromRankings = playerRankingList[listIndex];
                        //removeQuoteFromRankings = removeQuoteFromRankings.Remove(0, 1);
                        //removeQuoteFromRankings = removeQuoteFromRankings.Remove(removeQuoteFromRankings.Length - 1, 1);
                        p.Value.TradeValueChart = playerTradeValueList[listIndex];
                    }

                }
            }
        }


        //public void LoadTradeValueChart(Dictionary<string, PlayerData> players)
        //{
        //    using (var reader = new StreamReader("C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\Value_Scores_Full_Data_data.csv"))
        //    {
        //        while (!reader.EndOfStream)
        //        {
        //            var line = reader.ReadLine();
        //            var values = line.Split(',');
        //            string temp = values[0];
        //            foreach(var player in players)
        //            {
        //                if(players.ContainsValue(temp))
        //                {

        //                }
        //            }
        //        }
        //    }
        //}

        public List<Rosters> AverageTeamRanking(List<Rosters> rosters, Dictionary<string, PlayerData> players)
        {
            double totalTemp = 0.0;
            double qbTemp = 0.0;
            double rbTemp = 0.0;
            double wrTemp = 0.0;
            double teTemp = 0.0;
            int qbCount = 0, rbCount = 0, wrCount = 0, teCount = 0;
            double parseTemp = 0.0;

            foreach (Rosters ros in rosters)
            {
                foreach (string playerID in ros.Bench)
                {
                    if (players.ContainsKey(playerID))
                    {
                        PlayerData currentPlayer = new PlayerData();
                        currentPlayer = GetPlayerData(playerID, players);
                        if (currentPlayer.TradeValueChart != "")
                        {
                            parseTemp = Convert.ToDouble(currentPlayer.TradeValueChart);
                            System.Diagnostics.Debug.WriteLine("TESTING PLAYER: " + currentPlayer.FirstName + " " + currentPlayer.LastName + ": " + currentPlayer.TradeValueChart);
                            //totalTemp = totalTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                            totalTemp = totalTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                            
                            if (currentPlayer.Position.Contains("QB"))
                            {
                                qbTemp = qbTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                qbCount++;
                            }
                            if (currentPlayer.Position.Contains("RB"))
                            {
                                rbTemp = rbTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                rbCount++;
                            }
                            if (currentPlayer.Position.Contains("WR"))
                            {
                                wrTemp = wrTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                wrCount++;
                            }
                            if (currentPlayer.Position.Contains("TE"))
                            {
                                teTemp = teTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                teCount++;
                            }
                        }
                        else
                        {
                            currentPlayer.FantasyProsRanking = "300";
                            currentPlayer.TradeValueChart = "1.0";
                            //System.Diagnostics.Debug.WriteLine("TESTING PLAYER: " + currentPlayer.FirstName + " " + currentPlayer.LastName + ": " + currentPlayer.TradeValueChart);
                            totalTemp = totalTemp + Convert.ToDouble(currentPlayer.TradeValueChart);

                            if (currentPlayer.Position.Contains("QB"))
                            {
                                qbTemp = qbTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                qbCount++;
                            }
                            if (currentPlayer.Position.Contains("RB"))
                            {
                                rbTemp = rbTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                rbCount++;
                            }
                            if (currentPlayer.Position.Contains("WR"))
                            {
                                wrTemp = wrTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                wrCount++;
                            }
                            if (currentPlayer.Position.Contains("TE"))
                            {
                                teTemp = teTemp + Convert.ToDouble(currentPlayer.TradeValueChart);
                                teCount++;
                            }
                        }

                    }
                }

                //ros.TeamRankingAverage = totalTemp / ros.Bench.Count();
                //ros.QBRankingAverage = qbTemp / qbCount;
                //ros.RBRankingAverage = rbTemp / rbCount;
                //ros.WRRankingAverage = wrTemp / wrCount;
                //ros.TERankingAverage = teTemp / teCount;

                ros.TeamRankingAverage = totalTemp;
                ros.QBRankingAverage = qbTemp;
                ros.RBRankingAverage = rbTemp;
                ros.WRRankingAverage = wrTemp;
                ros.TERankingAverage = teTemp;

                //System.Diagnostics.Debug.WriteLine("Roster: " + ros.RosterID);
                //System.Diagnostics.Debug.WriteLine("AVERAGE: " + ros.TeamRankingAverage);
                //System.Diagnostics.Debug.WriteLine("QB AVERAGE: " + ros.QBRankingAverage);
                //System.Diagnostics.Debug.WriteLine("RB AVERAGE: " + ros.RBRankingAverage);
                //System.Diagnostics.Debug.WriteLine("WR AVERAGE: " + ros.WRRankingAverage);
                //System.Diagnostics.Debug.WriteLine("TE AVERAGE: " + ros.TERankingAverage);



                totalTemp = 0;
                qbTemp = 0;
                rbTemp = 0;
                wrTemp = 0;
                teTemp = 0;
                qbCount = 0;
                rbCount = 0;
                wrCount = 0;
                teCount = 0;
            }
            return rosters;
        }

        public PlayerData GetPlayerData(string playerID, Dictionary<string, PlayerData> playerList)
        {
            if (playerList.ContainsKey(playerID))
            {
                return playerList[playerID];
            }
            return null;
        }
    }
}
