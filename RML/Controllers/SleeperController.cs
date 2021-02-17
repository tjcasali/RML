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
using System.Web.UI.WebControls;

namespace RML.Controllers
{
    public class SleeperController : Controller
    {
        public List<SleeperUsers> sleeperUsers = new List<SleeperUsers>();
        public static List<Rosters> sleeperRosters = new List<Rosters>();
        public Dictionary<string, PlayerData> playerList = new Dictionary<string, PlayerData>();
        public List<KeepTradeCut> keepTradeCutList = new List<KeepTradeCut>();
        public UserInfo leagueInformation = new UserInfo();
        public List<TradedPick> tradedPicks = new List<TradedPick>();
        public string lastScrapeDate;

        // GET: Sleeper
        public ActionResult Index()
        {
            return View();
        }

        #region ActionResults

        //[Route("Sleeper/DisplayLeague/{leagueId}")]
        public async Task<ActionResult> DisplayLeague(string leagueID)
        {
            if (leagueID != String.Empty)
            {
                try
                {
                    leagueInformation = await GetLeagueInformation(leagueID);
                    sleeperUsers = await GetUsers(leagueID);
                    sleeperRosters = await GetRosters(leagueID);
                    playerList = GetPlayers();
                    tradedPicks = await GetTradedDraftPicks(leagueID);
                }
                catch
                {
                    return RedirectToAction("InvalidLeagueID");
                }
            }
            else
            {
                return RedirectToAction("InvalidLeagueID");
            }

            LinkUsersAndRosters(sleeperUsers, sleeperRosters);

            lastScrapeDate = GetPreviousScrapeDate(lastScrapeDate);

            //LoadSleeperPlayersTextFile();

            //keepTradeCutList = ScrapeRankings(playerList);

            ScrapeRankings(lastScrapeDate);

            ScrapeSFRankings(lastScrapeDate);

            playerList = LoadRankings(playerList, keepTradeCutList, leagueInformation);

            sleeperRosters = AverageTeamRanking(sleeperRosters, playerList);

            AddPlayerNamesToRosters(sleeperRosters, playerList);

            sleeperRosters = RankPositionGroups(sleeperRosters);

            sleeperRosters = SortRostersByRanking(sleeperRosters);

            sleeperRosters = RankStartingLineups(sleeperRosters, leagueInformation);

            OrderStartingLineupRanking(sleeperRosters);

            var viewModel = new DisplayLeagueViewModel
            {
                Rosters = sleeperRosters,
                UserInfo = leagueInformation,
                LastScrapeDate = lastScrapeDate
            };

            return View(viewModel);
        }
        public ActionResult TeamBreakdown(string leagueID, string name)
        {
            foreach (var ros in sleeperRosters)
            {
                if (ros.DisplayName == name)
                {
                    ros.SelectedRoster = 1;
                    continue;
                }
            }

            sleeperRosters = FindTradeTargets(sleeperRosters);

            var viewModel = new TeamBreakdownViewModel
            {
                Rosters = sleeperRosters,
                SelectedRosterVM = sleeperRosters.Find(x => x.SelectedRoster == 1),
                LeagueID = leagueID
            };

            return View(viewModel);
        }

        public ActionResult InvalidLeagueID()
        {
            return View();
        }

        #endregion

        #region Get Rosters/Users/Players

        public static async Task<UserInfo> GetLeagueInformation(string leagueID)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            UserInfo leagueInfo= JsonConvert.DeserializeObject<UserInfo>(responseBody);
            bool isSuper = false;

            foreach(string position in leagueInfo.LeagueRosterPositions)
            {
                if(position == "QB")
                {
                    leagueInfo.QBCount++;
                }
                if (position == "RB")
                {
                    leagueInfo.RBCount++;
                }
                if (position == "WR")
                {
                    leagueInfo.WRCount++;
                }
                if (position == "TE")
                {
                    leagueInfo.TECount++;
                }
                if (position == "FLEX")
                {
                    leagueInfo.FLEXCount++;
                }
                if (position == "SUPER_FLEX")
                {
                    leagueInfo.SUPERFLEXCount++;
                }
                if (position == "SUPER_FLEX" || leagueInfo.QBCount > 1)
                {
                    isSuper = true;
                }
            }

            leagueInfo.SuperFlex = isSuper;
            leagueInfo.LeagueID = leagueID;

            return leagueInfo;
        }

        /// Get Users
        /// Take in user submitted league ID and put that into the Sleeper API to return the Users in the league
        public static async Task<List<SleeperUsers>> GetUsers(string leagueID)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID + "/users");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<SleeperUsers> userList = JsonConvert.DeserializeObject<List<SleeperUsers>>(responseBody);

            return userList;

        }

        public static async Task<List<Rosters>> GetRosters(string leagueID)
        {

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID + "/rosters");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<Rosters> rosters = JsonConvert.DeserializeObject<List<Rosters>>(responseBody);

            return rosters;
        }

        public static async Task<List<TradedPick>> GetTradedDraftPicks(string leagueID)
        {

            HttpClient client = new HttpClient();

            //My roster ID: 325829344368812032

            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID + "/traded_picks");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<TradedPick> tradedPicks = JsonConvert.DeserializeObject<List<TradedPick>>(responseBody);

            return tradedPicks;
        }

        public async void LoadSleeperPlayersTextFile()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await
            client.GetAsync("https://api.sleeper.app/v1/players/nfl");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            string data = Server.MapPath("~/Data/SleeperGetPlayers.txt");
            System.IO.File.WriteAllText(data, responseBody);
        }

        public Dictionary<string, PlayerData> GetPlayers()
        {
            string data = Server.MapPath("~/Data/SleeperGetPlayers.txt");
            string json = System.IO.File.ReadAllText(data);

            Dictionary<string, PlayerData> playerList = JsonConvert.DeserializeObject<Dictionary<string, PlayerData>>(json);

            return playerList;
        }
        #endregion

        #region Add Owner IDs and Player Values to Rosters
        public void LinkUsersAndRosters(List<SleeperUsers> users, List<Rosters> rosters)
        {
            foreach (Rosters ros in rosters)
            {
                ros.DraftPicks.Add("2021 1st");
                ros.DraftPicks.Add("2021 2nd");
                ros.DraftPicks.Add("2021 3rd"); 
                ros.DraftPicks.Add("2021 4th");
                ros.DraftPicks.Add("2022 1st");
                ros.DraftPicks.Add("2022 2nd");
                ros.DraftPicks.Add("2022 3rd");
                ros.DraftPicks.Add("2022 4th");
                ros.DraftPicks.Add("2023 1st");
                ros.DraftPicks.Add("2023 2nd");
                ros.DraftPicks.Add("2023 3rd");
                ros.DraftPicks.Add("2023 4th");
                ros.DraftPicks.Add("2024 1st");
                ros.DraftPicks.Add("2024 2nd");
                ros.DraftPicks.Add("2024 3rd");
                ros.DraftPicks.Add("2024 4th");
                ros.DraftPicks.Add("2025 1st");
                ros.DraftPicks.Add("2025 2nd");
                ros.DraftPicks.Add("2025 3rd");
                ros.DraftPicks.Add("2025 4th");
                foreach (SleeperUsers su in users)
                {
                    if (su.UserID == ros.OwnerID)
                    {
                        su.RosterID = ros.RosterID;
                        ros.DisplayName = su.DisplayName;
                    }
                }
            }
        }

        public void AddPlayerNamesToRosters(List<Rosters> rosters, Dictionary<string, PlayerData> players)
        {
            List<string> tempPlayersList;
            List<string> tempPlayerRankingsList;
            Dictionary<string, POR> tempPORDict;
            POR tempPOREntry;
            string temp = "";


            PlayerData tempPlayer = new PlayerData();

            foreach (Rosters ros in rosters)
            {
                tempPlayersList = new List<string>();
                tempPlayerRankingsList = new List<string>();
                tempPORDict = new Dictionary<string, POR>();
                foreach (string p in ros.Bench)
                {
                    tempPOREntry = new POR();
                    temp = players[p].FirstName + " " + players[p].LastName;

                    tempPlayer = GetPlayerData(p, players);
                    tempPlayerRankingsList.Add(tempPlayer.KeepTradeCutValue);

                    tempPlayersList.Add(temp);

                    tempPOREntry.PORName = temp;
                    tempPOREntry.PORPosition = tempPlayer.Position;
                    tempPOREntry.PORValue = Convert.ToInt32(tempPlayer.KeepTradeCutValue);

                    tempPORDict.Add(p, tempPOREntry);
                    //ros.PlayersOnRoster.Add(p, tempPOR);
                    
                    
                }
                ros.PlayerNames = tempPlayersList;
                ros.PlayerTradeValues = tempPlayerRankingsList;
                ros.PlayersOnRoster = tempPORDict;
            }
        }
        #endregion

        #region Ranking Functions

        public Dictionary<string, PlayerData> LoadRankings(Dictionary<string, PlayerData> players, List<KeepTradeCut> ktc, UserInfo leagueInfo)
        {
            string sr = "";

            if (leagueInfo.SuperFlex)
                sr = Server.MapPath("~/Data/KTCScrapeSF.csv");
            else
                sr = Server.MapPath("~/Data/KTCScrape.csv");

            using (var reader = new StreamReader(sr))
            {
                List<string> playerNameList = new List<string>();
                List<string> playerPositionList = new List<string>();
                List<string> playerTeamList = new List<string>();
                List<string> playerKeepTradeCutList = new List<string>();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    playerNameList.Add(values[0]);
                    playerPositionList.Add(values[1]);
                    playerTeamList.Add(values[2]);
                    playerKeepTradeCutList.Add(values[3]);
                }

                foreach (var p in players)
                {
                    string temp = "";
                    int tempIndex = 0;

                    string firstNameTemp = p.Value.FirstName.Replace(".", string.Empty);
                    string lastNameTemp = p.Value.LastName.Replace(".", string.Empty);

                    temp = '"' + firstNameTemp + " " + lastNameTemp + '"';
                    temp = temp.Remove(0, 1);
                    temp = temp.Remove(temp.Length - 1, 1);

                    if (playerNameList.Contains(temp))
                    {
                        tempIndex = playerNameList.IndexOf(temp);
                        p.Value.KeepTradeCutValue = playerKeepTradeCutList[tempIndex];
                    }

                }

                return players;
            }
        }

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
                        if (currentPlayer.KeepTradeCutValue != "")
                        {
                            parseTemp = Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                            //System.Diagnostics.Debug.WriteLine("TESTING PLAYER: " + currentPlayer.FirstName + " " + currentPlayer.LastName + ": " + currentPlayer.KeepTradeCutValue);
                            //totalTemp = totalTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                            totalTemp = totalTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);

                            if (currentPlayer.Position.Contains("QB"))
                            {
                                qbTemp = qbTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                qbCount++;
                            }
                            if (currentPlayer.Position.Contains("RB"))
                            {
                                rbTemp = rbTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                rbCount++;
                            }
                            if (currentPlayer.Position.Contains("WR"))
                            {
                                wrTemp = wrTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                wrCount++;
                            }
                            if (currentPlayer.Position.Contains("TE"))
                            {
                                teTemp = teTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                teCount++;
                            }
                        }
                        else
                        {
                            currentPlayer.KeepTradeCutValue = "1.0";
                            //System.Diagnostics.Debug.WriteLine("TESTING PLAYER: " + currentPlayer.FirstName + " " + currentPlayer.LastName + ": " + currentPlayer.KeepTradeCutValue);
                            totalTemp = totalTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);

                            if (currentPlayer.Position.Contains("QB"))
                            {
                                qbTemp = qbTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                qbCount++;
                            }
                            if (currentPlayer.Position.Contains("RB"))
                            {
                                rbTemp = rbTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                rbCount++;
                            }
                            if (currentPlayer.Position.Contains("WR"))
                            {
                                wrTemp = wrTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                wrCount++;
                            }
                            if (currentPlayer.Position.Contains("TE"))
                            {
                                teTemp = teTemp + Convert.ToDouble(currentPlayer.KeepTradeCutValue);
                                teCount++;
                            }
                        }

                    }
                }

                ros.TeamRankingAverage = totalTemp;
                ros.QBRankingAverage = qbTemp;
                ros.RBRankingAverage = rbTemp;
                ros.WRRankingAverage = wrTemp;
                ros.TERankingAverage = teTemp;

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
        public string GetPreviousScrapeDate(string date)
        {
            string path = Server.MapPath("~/Data/LastScrapeDate.txt");
            date = System.IO.File.ReadAllText(path);

            return date;
        }

        public void ScrapeRankings(string previousScrapeDate)
        {
            string newScrapeDate = DateTime.Now.ToString("MM-dd-yyyy");

            if (previousScrapeDate != newScrapeDate)
            {
                string url = "https://keeptradecut.com/dynasty-rankings?page=0&filters=QB|WR|RB|TE|RDP&format=1";

                HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(url);

                var nameTable = doc.DocumentNode.SelectNodes("//div[@class='player-name']");
                var valueTable = doc.DocumentNode.SelectNodes("//div[@class='value']");
                var positionTeamTable = doc.DocumentNode.SelectNodes("//div[@class='position-team']");


                List<string> nameList = new List<string>();
                List<string> valueList = new List<string>();
                List<string> positionList = new List<string>();
                List<string> teamList = new List<string>();

                string temp, positionTemp, teamTemp = "";
                int tempSize = 0;

                foreach (var name in nameTable.Skip(1))
                {
                    temp = name.InnerText;
                    temp = temp.Trim();
                    temp = temp.Replace("//n", "");
                    tempSize = temp.Length;
                    if (temp.EndsWith("FA"))
                    {
                        teamTemp = temp.Substring(temp.Length - 2);
                        temp = temp.Substring(0, tempSize - 2);
                        tempSize = temp.Length;
                        teamList.Add(teamTemp);
                    }
                    else
                    {
                        teamTemp = temp.Substring(temp.Length - 3);
                        temp = temp.Substring(0, tempSize - 3);
                        tempSize = temp.Length;
                        teamList.Add(teamTemp);
                    }
                    if (temp.EndsWith("R"))
                    {
                        temp = temp.Substring(0, tempSize - 1);
                        temp = temp.Trim();
                        temp = temp.Replace("\\n", "");
                    }
                    if (temp.Contains("Jr."))
                    {
                        temp = temp.Replace("Jr.", "");
                        temp = temp.Trim();
                    }
                    if (temp.Contains("."))
                    {
                        temp = temp.Replace(".", String.Empty);
                    }
                    if (temp.Contains("&#x27;"))
                    {
                        temp = temp.Replace("&#x27;", "'");
                    }
                    if (temp.Contains("Lamical"))
                        temp = "La'Mical Perine";
                    if (temp.Contains("JaMycal"))
                        temp = "Jamycal Hasty";
                    if (temp.Contains("Herndon"))
                        temp = "Christopher Herndon";
                    nameList.Add(temp);
                }
                foreach (var value in valueTable.Skip(1))
                {
                    temp = value.InnerText;
                    temp = temp.Trim();
                    temp = temp.Replace("//n", "");
                    valueList.Add(temp);
                }
                foreach (var positionteam in positionTeamTable.Skip(1))
                {
                    temp = positionteam.InnerText;
                    temp = temp.Trim();
                    temp = temp.Replace("//n", "");

                    positionTemp = temp.Substring(0, 2);
                    if (positionTemp == "RD")
                        positionTemp = "NA";

                    temp = temp.Remove(0, 3);

                    teamTemp = temp;

                    positionList.Add(positionTemp);
                }


                string tempName, tempValue, tempPosition, tempTeam;
                int count = 0;
                List<Player> ktcList = new List<Player>();

                Player newKtc = new Player();

                foreach (var p in nameList)
                {
                    newKtc = new Player();
                    tempName = p;
                    if (valueList.ElementAt(count) != null)
                    {
                        tempValue = valueList.ElementAt(count);
                    }
                    else
                    {
                        tempValue = "NA";
                    }
                    if (teamList.ElementAt(count) != null)
                    {
                        tempTeam = teamList.ElementAt(count);
                    }
                    else
                    {
                        tempTeam = "NA";
                    }
                    if (positionList.ElementAt(count) != null)
                    {
                        tempPosition = positionList.ElementAt(count);
                    }
                    else
                    {
                        tempPosition = "NA";
                    }
                    newKtc.Name = tempName;
                    newKtc.Value = tempValue;
                    newKtc.Position = tempPosition;
                    newKtc.Team = tempTeam;

                    ktcList.Add(newKtc);

                    count++;
                }

                string path = Server.MapPath("~/Data/KTCScrape.csv");
                //string fileName = "C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\KTCScrape.csv";
                string newLine = "";

                System.IO.File.WriteAllText(path, String.Empty);
                foreach (var p in ktcList)
                {
                    newLine = "";
                    newLine = p.Name + "," + p.Position + "," + p.Team + "," + p.Value + Environment.NewLine;
                    System.IO.File.AppendAllText(path, newLine);
                }

                //string filename = "C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\LastScrapeDate.txt";
                string data = Server.MapPath("~/Data/LastScrapeDate.txt");
                System.IO.File.WriteAllText(data, newScrapeDate);
            }
            //return newScrapeDate;
        }

        public void ScrapeSFRankings(string previousScrapeDate)
        {
            string newScrapeDate = DateTime.Now.ToString("MM-dd-yyyy");

            if (previousScrapeDate != newScrapeDate)
            {
                string url = "https://keeptradecut.com/dynasty-rankings?format=2";

                HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(url);

                var nameTable = doc.DocumentNode.SelectNodes("//div[@class='player-name']");
                var valueTable = doc.DocumentNode.SelectNodes("//div[@class='value']");
                var positionTeamTable = doc.DocumentNode.SelectNodes("//div[@class='position-team']");


                List<string> nameList = new List<string>();
                List<string> valueList = new List<string>();
                List<string> positionList = new List<string>();
                List<string> teamList = new List<string>();

                string temp, positionTemp, teamTemp = "";
                int tempSize = 0;

                foreach (var name in nameTable.Skip(1))
                {
                    temp = name.InnerText;
                    temp = temp.Trim();
                    temp = temp.Replace("//n", "");
                    tempSize = temp.Length;
                    if (temp.EndsWith("FA"))
                    {
                        teamTemp = temp.Substring(temp.Length - 2);
                        temp = temp.Substring(0, tempSize - 2);
                        tempSize = temp.Length;
                        teamList.Add(teamTemp);
                    }
                    else
                    {
                        teamTemp = temp.Substring(temp.Length - 3);
                        temp = temp.Substring(0, tempSize - 3);
                        tempSize = temp.Length;
                        teamList.Add(teamTemp);
                    }
                    if (temp.EndsWith("R"))
                    {
                        temp = temp.Substring(0, tempSize - 1);
                        temp = temp.Trim();
                        temp = temp.Replace("\\n", "");
                    }
                    if (temp.Contains("Jr."))
                    {
                        temp = temp.Replace("Jr.", "");
                        temp = temp.Trim();
                    }
                    if (temp.Contains("."))
                    {
                        temp = temp.Replace(".", String.Empty);
                    }
                    if (temp.Contains("&#x27;"))
                    {
                        temp = temp.Replace("&#x27;", "'");
                    }
                    if (temp.Contains("Lamical"))
                    {
                        temp = "La'Mical Perine";
                    }
                    if (temp.Contains("JaMycal"))
                    {
                        temp = "Jamycal Hasty";
                    }
                    if (temp.Contains("Irv Smith"))
                    {
                        temp = "Irv Smith";
                    }
                    nameList.Add(temp);
                }
                foreach (var value in valueTable.Skip(1))
                {
                    temp = value.InnerText;
                    temp = temp.Trim();
                    temp = temp.Replace("//n", "");
                    valueList.Add(temp);
                }
                foreach (var positionteam in positionTeamTable.Skip(1))
                {
                    temp = positionteam.InnerText;
                    temp = temp.Trim();
                    temp = temp.Replace("//n", "");

                    positionTemp = temp.Substring(0, 2);
                    if (positionTemp == "RD")
                        positionTemp = "NA";

                    temp = temp.Remove(0, 3);

                    teamTemp = temp;

                    positionList.Add(positionTemp);
                }


                string tempName, tempValue, tempPosition, tempTeam;
                int count = 0;
                List<Player> ktcList = new List<Player>();

                Player newKtc = new Player();

                foreach (var p in nameList)
                {
                    newKtc = new Player();
                    tempName = p;
                    if (valueList.ElementAt(count) != null)
                    {
                        tempValue = valueList.ElementAt(count);
                    }
                    else
                    {
                        tempValue = "NA";
                    }
                    if (teamList.ElementAt(count) != null)
                    {
                        tempTeam = teamList.ElementAt(count);
                    }
                    else
                    {
                        tempTeam = "NA";
                    }
                    if (positionList.ElementAt(count) != null)
                    {
                        tempPosition = positionList.ElementAt(count);
                    }
                    else
                    {
                        tempPosition = "NA";
                    }
                    newKtc.Name = tempName;
                    newKtc.Value = tempValue;
                    newKtc.Position = tempPosition;
                    newKtc.Team = tempTeam;

                    ktcList.Add(newKtc);

                    count++;
                }

                //string fileName = "C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\KTCScrapeSF.csv";
                string path = Server.MapPath("~/Data/KTCScrapeSF.csv");
                string newLine = "";

                System.IO.File.WriteAllText(path, String.Empty);
                foreach (var p in ktcList)
                {
                    newLine = "";
                    newLine = p.Name + "," + p.Position + "," + p.Team + "," + p.Value + Environment.NewLine;
                    System.IO.File.AppendAllText(path, newLine);
                }

                string data = Server.MapPath("~/Data/LastScrapeDate.txt");
                //string filename = "C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\LastScrapeDate.txt";
                System.IO.File.WriteAllText(data, newScrapeDate);
            }
            //return newScrapeDate;
        }

        public List<Rosters> SortRostersByRanking(List<Rosters> rosters)
        {
            List<Rosters> sortedRosters = rosters.OrderByDescending(o => o.TeamRankingAverage).ToList();
            return sortedRosters;
        }

        public void OrderStartingLineupRanking(List<Rosters> rosters)
        {
            List<Rosters> sortedRosters = rosters.OrderByDescending(o => o.TeamStartingTotal).ToList();
            int count = 0;
            foreach (var ros in sortedRosters)
            {
                count++;

                ros.StartingTeamRank = count;
            }

        }

        public List<Rosters> RankStartingLineups(List<Rosters> rosters, UserInfo leagueInfo)
        {
            int positionCounter = 0;

            double startingQBTotal = 0.0;
            double startingRBTotal = 0.0;
            double startingWRTotal = 0.0;
            double startingTETotal = 0.0;
            double startingFLEXTotal = 0.0;
            double startingSUPERFLEXTotal = 0.0;

            List<string> skippedPlayerNames = new List<string>();
            List<string> startingPlayerNames = new List<string>();
            List<string> flexPlayerNames = new List<string>();
            List<string> superflexPlayerNames = new List<string>();



            foreach (var ros in rosters)
            {
                foreach (var player in ros.PlayersOnRoster.Where(o => o.Value.PORPosition == "QB").OrderByDescending(o => o.Value.PORValue))
                {
                    skippedPlayerNames.Add(player.Value.PORName);
                    startingPlayerNames.Add(player.Value.PORName);
                    positionCounter++;
                    if (positionCounter == leagueInfo.QBCount)
                    {
                        startingQBTotal += player.Value.PORValue;
                        positionCounter = 0;
                        break;
                    }
                    else
                    {
                        startingQBTotal += player.Value.PORValue;
                    }
                }
                foreach (var player in ros.PlayersOnRoster.Where(o => o.Value.PORPosition == "RB").OrderByDescending(o => o.Value.PORValue))
                {
                    skippedPlayerNames.Add(player.Value.PORName);
                    startingPlayerNames.Add(player.Value.PORName);
                    positionCounter++;
                    if (positionCounter == leagueInfo.RBCount)
                    {
                        startingRBTotal += player.Value.PORValue;
                        positionCounter = 0;
                        break;
                    }
                    else
                    {
                        startingRBTotal += player.Value.PORValue;
                    }
                }
                foreach (var player in ros.PlayersOnRoster.Where(o => o.Value.PORPosition == "WR").OrderByDescending(o => o.Value.PORValue))
                {
                    skippedPlayerNames.Add(player.Value.PORName);
                    startingPlayerNames.Add(player.Value.PORName);
                    positionCounter++;
                    if (positionCounter == leagueInfo.WRCount)
                    {
                        startingWRTotal += player.Value.PORValue;
                        positionCounter = 0;
                        break;
                    }
                    else
                    {
                        startingWRTotal += player.Value.PORValue;
                    }
                }
                foreach (var player in ros.PlayersOnRoster.Where(o => o.Value.PORPosition == "TE").OrderByDescending(o => o.Value.PORValue))
                {
                    skippedPlayerNames.Add(player.Value.PORName);
                    startingPlayerNames.Add(player.Value.PORName);
                    positionCounter++;
                    if (positionCounter == leagueInfo.TECount)
                    {
                        startingTETotal += player.Value.PORValue;
                        positionCounter = 0;
                        break;
                    }
                    else
                    {
                        startingTETotal += player.Value.PORValue;
                    }
                }
                foreach (var player in ros.PlayersOnRoster.OrderByDescending(o => o.Value.PORValue))
                {
                    if (leagueInfo.SUPERFLEXCount != 0)
                    {
                        if (skippedPlayerNames.Contains(player.Value.PORName))
                        {
                            continue;
                        }
                        skippedPlayerNames.Add(player.Value.PORName);
                        flexPlayerNames.Add(player.Value.PORName);

                        positionCounter++;
                        if (positionCounter == leagueInfo.FLEXCount)
                        {
                            skippedPlayerNames.Add(player.Value.PORName);
                            startingFLEXTotal += player.Value.PORValue;
                            positionCounter = 0;
                            break;
                        }
                        else
                        {
                            startingFLEXTotal += player.Value.PORValue;
                        }
                    }
                    else
                    {
                        if (skippedPlayerNames.Contains(player.Value.PORName) || player.Value.PORPosition == "QB")
                        {
                            continue;
                        }
                        skippedPlayerNames.Add(player.Value.PORName);
                        flexPlayerNames.Add(player.Value.PORName);

                        positionCounter++;
                        if (positionCounter == leagueInfo.FLEXCount)
                        {
                            skippedPlayerNames.Add(player.Value.PORName);
                            startingFLEXTotal += player.Value.PORValue;
                            positionCounter = 0;
                            break;
                        }
                        else
                        {
                            startingFLEXTotal += player.Value.PORValue;
                        }
                    }

                }


                ros.QBStartingTotal = startingQBTotal;
                ros.RBStartingTotal = startingRBTotal;
                ros.WRStartingTotal = startingWRTotal;
                ros.TEStartingTotal = startingTETotal;
                ros.FLEXStartingTotal = startingFLEXTotal;
                ros.TeamStartingTotal = startingQBTotal + startingRBTotal + startingWRTotal + startingTETotal + startingFLEXTotal;

                ros.StartingPlayerList = startingPlayerNames;
                ros.StartingFlexList = flexPlayerNames;

                positionCounter = 0;

                startingQBTotal = 0.0;
                startingRBTotal = 0.0;
                startingWRTotal = 0.0;
                startingTETotal = 0.0;
                startingFLEXTotal = 0.0;
            }
            return rosters;
        }

        public List<Rosters> RankPositionGroups(List<Rosters> rosters)
        {
            List<Rosters> rankedRosters = new List<Rosters>();

            rankedRosters = rosters.OrderByDescending(o => o.QBRankingAverage).ToList();
            int count = 1;
            foreach (var ros in rankedRosters)
            {
                ros.QBRank = count;
                count++;
            }

            rankedRosters = rosters.OrderByDescending(o => o.RBRankingAverage).ToList();
            count = 1;
            foreach (var ros in rankedRosters)
            {
                ros.RBRank = count;
                count++;
            }

            rankedRosters = rosters.OrderByDescending(o => o.WRRankingAverage).ToList();
            count = 1;
            foreach (var ros in rankedRosters)
            {
                ros.WRRank = count;
                count++;
            }

            rankedRosters = rosters.OrderByDescending(o => o.TERankingAverage).ToList();
            count = 1;
            foreach (var ros in rankedRosters)
            {
                ros.TERank = count;
                count++;
            }

            return rankedRosters;
        }

        #endregion

        public List<Rosters> FindTradeTargets(List<Rosters> rosters)
        {
            var tempRoster = rosters.Find(x => x.SelectedRoster == 1);

            List<string> tempTradeCandidates = new List<string>();

            bool qbAdv;
            bool rbAdv;
            bool wrAdv;
            bool teAdv;

            foreach (var ros in rosters)
            {

                if (ros.RosterID != tempRoster.RosterID)
                {
                    qbAdv = false;
                    rbAdv = false;
                    wrAdv = false;
                    teAdv = false;

                    //if(ros.QBRankingAverage > tempRoster.QBRankingAverage || ros.RBRankingAverage > tempRoster.RBRankingAverage || ros.WRRankingAverage > tempRoster.WRRankingAverage || ros.TERankingAverage > tempRoster.TERankingAverage)
                    if (ros.QBRankingAverage > tempRoster.QBRankingAverage)
                        qbAdv = true;
                    if (ros.RBRankingAverage > tempRoster.RBRankingAverage)
                        rbAdv = true;
                    if (ros.WRRankingAverage > tempRoster.WRRankingAverage)
                        wrAdv = true;
                    if (ros.TERankingAverage > tempRoster.TERankingAverage)
                        teAdv = true;

                    if (qbAdv && rbAdv && wrAdv && teAdv || !qbAdv && !rbAdv && !wrAdv && !teAdv)
                    {
                        continue;
                    }

                    if (qbAdv || rbAdv || wrAdv || teAdv)
                    {
                        tempTradeCandidates.Add(ros.DisplayName);
                    }
                }
            }
            tempRoster.TradeCandidates = tempTradeCandidates;

            return rosters;
        }

        public List<Rosters> TradedDraftPicks(List<Rosters> rosters, List<TradedPick> tp)
        {
            /**TODO
             * So TradedPick has Season Round Roster ID Previous Owner ID and Owner ID. 
             * Add Wins and Losses to TradedPick or do a compare on wins and losses and assign an Early Middle Late as a string in Traded Pick
             * If they traded a pick that they don't have check the rest of the picks for the owner ID?
             * wins and losses are zero already but record has previous years WL record
             * Maybe just ask them to import their draft ID?
             * 
             * JJJJJKKKK, Get all Drafts for League, get Draft ID, gives draft order and number of rounds
             **/



            return rosters;
        }

        #region COMMENTED OUT FUNCTIONS

        //public Dictionary<string, PlayerData> LoadRankings(Dictionary<string, PlayerData> players, List<KeepTradeCut> ktc)
        //{

        //    using (var reader = new StreamReader("C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\Value_Scores_Full_Data_data.csv"))
        //    {
        //        List<string> playerNameList = new List<string>();
        //        List<string> playerPositionList = new List<string>();
        //        List<string> playerKeepTradeCutList = new List<string>();

        //        while (!reader.EndOfStream)
        //        {
        //            var line = reader.ReadLine();
        //            var values = line.Split(',');

        //            playerNameList.Add(values[0]);
        //            playerPositionList.Add(values[1]);
        //        }

        //        foreach (var p in players)
        //        {
        //            string temp = "";
        //            string tempKTCValue = "";
        //            KeepTradeCut tempKTC = new KeepTradeCut();
        //            string firstNameTemp = p.Value.FirstName.Replace(".", string.Empty);
        //            string lastNameTemp = p.Value.LastName.Replace(".", string.Empty);
        //            temp = '"' + firstNameTemp + " " + lastNameTemp + '"';
        //            temp = temp.Remove(0, 1);
        //            temp = temp.Remove(temp.Length - 1, 1);

        //            if (playerNameList.Contains(temp))
        //            {
        //                if (ktc.Any(a => a.PlayerName == temp))
        //                {
        //                    tempKTC = ktc.Find(a => a.PlayerName == temp);
        //                    tempKTCValue = tempKTC.Value;
        //                    p.Value.KeepTradeCutValue = tempKTCValue;

        //                }
        //            }

        //        }

        //        return players;
        //    }
        //}

        //public List<KeepTradeCut> ScrapeRankings(Dictionary<string, PlayerData> players)
        //{
        //    string url = "https://keeptradecut.com/dynasty-rankings?page=0&filters=QB|WR|RB|TE|RDP&format=1";

        //    HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
        //    HtmlAgilityPack.HtmlDocument doc = web.Load(url);

        //    var nameTable = doc.DocumentNode.SelectNodes("//div[@class='player-name']");
        //    var valueTable = doc.DocumentNode.SelectNodes("//div[@class='value']");

        //    List<string> nameList = new List<string>();
        //    List<string> valueList = new List<string>();
        //    string temp = "";
        //    int tempSize = 0;
        //    foreach (var name in nameTable)
        //    {
        //        temp = name.InnerText;
        //        temp = temp.Trim();
        //        temp = temp.Replace("//n", "");
        //        tempSize = temp.Length;
        //        System.Diagnostics.Debug.WriteLine(temp);
        //        if (temp.EndsWith("R"))
        //        {
        //            temp = temp.Substring(0, tempSize - 1);
        //            temp = temp.Trim();
        //            temp = temp.Replace("\\n", "");
        //        }
        //        if (temp.Contains("."))
        //        {
        //            temp = temp.Replace(".", String.Empty);
        //        }
        //        if (temp.Contains("&#x27;"))
        //        {
        //            temp = temp.Replace("&#x27;", "'");
        //        }
        //        nameList.Add(temp);
        //    }
        //    foreach (var value in valueTable)
        //    {
        //        temp = value.InnerText;
        //        temp = temp.Trim();
        //        temp = temp.Replace("//n", "");
        //        valueList.Add(temp);
        //    }

        //    string tempName, tempValue;
        //    int count = 0;
        //    List<KeepTradeCut> ktcList = new List<KeepTradeCut>();
        //    KeepTradeCut newKtc = new KeepTradeCut();

        //    foreach (var p in nameList)
        //    {
        //        newKtc = new KeepTradeCut();
        //        tempName = p;
        //        tempValue = valueList.ElementAt(count);
        //        count++;

        //        newKtc.PlayerName = tempName;
        //        newKtc.Value = tempValue;

        //        ktcList.Add(newKtc);
        //    }

        //    return ktcList;
        //}


        //public List<Rosters> SortPlayersOnRosterByValue(List<Rosters> rosters)
        //{
        //    List<Rosters> sortedRosters = new List<Rosters>();

        //    foreach(var ros in sortedRosters)
        //    {
        //        ros.PlayersOnRoster = ros.PlayersOnRoster.OrderByDescending(o => o.Value.PORValue);
        //    }
        //    return sortedRosters;
        //}
        #endregion
    }
}

