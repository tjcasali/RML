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
        //League IDs: Zacks (2QB) 650889409121607680

        public List<SleeperUsers> sleeperUsers = new List<SleeperUsers>();
        public static List<Rosters> sleeperRosters = new List<Rosters>();
        public Dictionary<string, PlayerData> playerList = new Dictionary<string, PlayerData>();
        public List<KeepTradeCut> keepTradeCutList = new List<KeepTradeCut>();
        public UserInfo leagueInformation = new UserInfo();

        // GET: Sleeper
        public ActionResult Index()
        {
            return View();
        }

        #region ActionResults

        //[Route("Sleeper/DisplayLeague/{leagueId}")]
        public async Task<ActionResult> DisplayLeague(string leagueID)
        {
            if(leagueID != String.Empty)
            {
                try
                {
                    leagueInformation = await GetLeagueInformation(leagueID);
                    sleeperUsers = await GetUsers(leagueID);
                    sleeperRosters = await GetRosters(leagueID);
                    playerList = GetPlayers();
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

            //LoadSleeperPlayersTextFile();

            //keepTradeCutList = ScrapeRankings(playerList);

            playerList = LoadRankings(playerList, keepTradeCutList, leagueInformation);

            sleeperRosters = AverageTeamRanking(sleeperRosters, playerList);

            AddPlayerNamesToRosters(sleeperRosters, playerList);

            sleeperRosters = RankPositionGroups(sleeperRosters);

            sleeperRosters = SortRostersByRanking(sleeperRosters);

            var viewModel = new DisplayLeagueViewModel
            {
                Rosters = sleeperRosters,
                UserInfo = leagueInformation
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

        public async Task<UserInfo> GetLeagueInformation(string leagueID)
        {
            HttpClient client = new HttpClient();


            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            UserInfo leagueInfo= JsonConvert.DeserializeObject<UserInfo>(responseBody);
            bool isSuper = false;
            int qbCount = 0;
            foreach(string position in leagueInfo.LeagueRosterPositions)
            {
                if(position == "QB")
                {
                    qbCount++;
                }
                if(position == "SUPER_FLEX" || qbCount > 1)
                {
                    isSuper = true;
                    break;
                }
            }

            leagueInfo.SuperFlex = isSuper;
            leagueInfo.LeagueID = leagueID;

            return leagueInfo;
        }

        /// Get Users
        /// Take in user submitted league ID and put that into the Sleeper API to return the Users in the league
        public async Task<List<SleeperUsers>> GetUsers(string leagueID)
        {
            HttpClient client = new HttpClient();

            //string leagueID = "515558249940426752";

            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID + "/users");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<SleeperUsers> userList = JsonConvert.DeserializeObject<List<SleeperUsers>>(responseBody);

            return userList;

        }

        public async Task<List<Rosters>> GetRosters(string leagueID)
        {

            HttpClient client = new HttpClient();

            //My roster ID: 325829344368812032
            //string leagueID = "515558249940426752";
            
            HttpResponseMessage response = await client.GetAsync("https://api.sleeper.app/v1/league/" + leagueID + "/rosters");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            List<Rosters> rosters = JsonConvert.DeserializeObject<List<Rosters>>(responseBody);

            return rosters;
        }

        public async void LoadSleeperPlayersTextFile()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await
            client.GetAsync("https://api.sleeper.app/v1/players/nfl");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            System.IO.File.WriteAllText(@"C:\Users\timca\source\repos\RML\RML\Data\SleeperGetPlayers.txt", responseBody);
        }

        public Dictionary<string, PlayerData> GetPlayers()
        {
            string json = System.IO.File.ReadAllText(@"C:\Users\timca\source\repos\RML\RML\Data\SleeperGetPlayers.txt");
            Dictionary<string, PlayerData> playerList = JsonConvert.DeserializeObject<Dictionary<string, PlayerData>>(json);

            return playerList;
        }
        #endregion

        #region Add Owner IDs and Player Values to Rosters
        public void LinkUsersAndRosters(List<SleeperUsers> users, List<Rosters> rosters)
        {
            foreach (Rosters ros in rosters)
            {
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

        //public List<KeepTradeCut> ScrapeRankings(Dictionary<string, PlayerData> players)
        //{
        //    string url = "https://keeptradecut.com/dynasty-rankings?page=0&filters=QB|WR|RB|TE|RDP&format=1";

        //    HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
        //    HtmlAgilityPack.HtmlDocument doc = web.Load(url);

        //    var nameTable = doc.DocumentNode.SelectNodes("//div[@class='player-name']");
        //    var valueTable = doc.DocumentNode.SelectNodes("//div[@class='value']");
        //    var positionTeamTable = doc.DocumentNode.SelectNodes("//div[@class='position-team']");


        //    List<string> nameList = new List<string>();
        //    List<string> valueList = new List<string>();
        //    List<string> positionList = new List<string>();
        //    List<string> teamList = new List<string>();

        //    string temp, positionTemp, teamTemp = "";
        //    int tempSize = 0;

        //    foreach (var name in nameTable.Skip(1))
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
        //        if (temp.Contains("Jr."))
        //        {
        //            temp = temp.Replace("Jr.", "");
        //        }
        //        if (temp.Contains("Lamical"))
        //        {
        //            temp = "La'Mical Perine";
        //        }
        //        if (temp.Contains("JaMycal"))
        //        {
        //            temp = "Jamycal Hasty";
        //        }
        //        nameList.Add(temp);
        //    }
        //    foreach (var value in valueTable.Skip(1))
        //    {
        //        temp = value.InnerText;
        //        temp = temp.Trim();
        //        temp = temp.Replace("//n", "");
        //        valueList.Add(temp);
        //    }
        //    foreach (var positionteam in positionTeamTable.Skip(1))
        //    {
        //        temp = positionteam.InnerText;
        //        temp = temp.Trim();
        //        temp = temp.Replace("//n", "");

        //        positionTemp = temp.Substring(0, 2);
        //        if (positionTemp == "RD")
        //            positionTemp = "NA";

        //        temp = temp.Remove(0, 3);

        //        teamTemp = temp;

        //        teamList.Add(teamTemp);
        //        positionList.Add(positionTemp);
        //    }


        //    string tempName, tempValue, tempPosition, tempTeam;
        //    int count = 0;
        //    List<Players> ktcList = new List<Players>();

        //    Players newKtc = new Players();

        //    foreach (var p in nameList)
        //    {
        //        newKtc = new Players();
        //        tempName = p;
        //        if (valueList.ElementAt(count) != null)
        //        {
        //            tempValue = valueList.ElementAt(count);
        //        }
        //        else
        //        {
        //            tempValue = "NA";
        //        }
        //        if (teamList.ElementAt(count) != null)
        //        {
        //            tempTeam = teamList.ElementAt(count);
        //        }
        //        else
        //        {
        //            tempTeam = "NA";
        //        }
        //        if (positionList.ElementAt(count) != null)
        //        {
        //            tempPosition = positionList.ElementAt(count);
        //        }
        //        else
        //        {
        //            tempPosition = "NA";
        //        }
        //        newKtc.Player = tempName;
        //        newKtc.Value = tempValue;
        //        newKtc.Position = tempPosition;
        //        newKtc.Team = tempTeam;

        //        ktcList.Add(newKtc);

        //        count++;
        //    }

        //    string fileName = "C:\\Users\\timca\\source\\repos\\RML\\RML\\KTCScrape.csv";
        //    string newLine = "";

        //    System.IO.File.WriteAllText(fileName, String.Empty);
        //    foreach (var p in ktcList)
        //    {
        //        newLine = "";
        //        newLine = p.Name + "," + p.Position + "," + p.Team + "," + p.Value + Environment.NewLine;
        //        System.IO.File.AppendAllText(fileName, newLine);
        //    }
        //}

        public Dictionary<string, PlayerData> LoadRankings(Dictionary<string, PlayerData> players, List<KeepTradeCut> ktc, UserInfo leagueInfo)
        {
            string sr = "";

            if (leagueInfo.SuperFlex)
                sr = "C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\KTCScrapeSF.csv";
            else
                sr = "C:\\Users\\timca\\source\\repos\\RML\\RML\\Data\\KTCScrape.csv";

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

        public List<Rosters> SortRostersByRanking(List<Rosters> rosters)
        {
            List<Rosters> sortedRosters = rosters.OrderByDescending(o => o.TeamRankingAverage).ToList();
            return sortedRosters;
        }

        public List<Rosters> RankPositionGroups(List<Rosters> rosters)
        {
            List<Rosters> rankedRosters = new List<Rosters>();

            rankedRosters = rosters.OrderByDescending(o => o.QBRankingAverage).ToList();
            int count = 1;
            foreach(var ros in rankedRosters)
            {
                ros.QBRanking = count;
                count++;
            }

            rankedRosters = rosters.OrderByDescending(o => o.RBRankingAverage).ToList();
            count = 1;
            foreach (var ros in rankedRosters)
            {
                ros.RBRanking = count;
                count++;
            }

            rankedRosters = rosters.OrderByDescending(o => o.WRRankingAverage).ToList();
            count = 1;
            foreach (var ros in rankedRosters)
            {
                ros.WRRanking = count;
                count++;
            }

            rankedRosters = rosters.OrderByDescending(o => o.TERankingAverage).ToList();
            count = 1;
            foreach (var ros in rankedRosters)
            {
                ros.TERanking = count;
                count++;
            }

            return rankedRosters;
        }

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
