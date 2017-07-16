using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Net;
using Discord;
using Discord.Commands;
using NinjaBotCore.Database;
using NinjaBotCore.Models.Steam;
using NinjaBotCore.Modules.Steam;
using Discord.WebSocket;
using NinjaBotCore.Models.RocketLeague;

namespace NinjaBotCore.Modules.RocketLeague
{
    public class RlCommands : ModuleBase
    {
        private Steam.Steam _steam = null;
        private RocketLeague _rl = null;
        private static ChannelCheck _cc = null;

        public RlCommands(Steam.Steam steam, ChannelCheck cc, RocketLeague rl)
        {
            try
            {
                if (_steam == null)
                {
                    _steam = steam;
                }
                if (_rl == null)
                {
                    _rl = rl;
                }                       
                if (_cc == null)
                {
                    _cc = cc;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"UNABLE TO CREATE CLASS ROCKET LEAGUE {ex.Message}");
            }
        }

        [Command("rlplatforms", RunMode = RunMode.Async)]
        [Summary("Get a list of platforms")]
        public async Task GetPlatforms()
        {
            var newRlApi = new RlStatsApi();
            var platforms = newRlApi.GetCurrentPlatforms();
            StringBuilder sb = new StringBuilder();
            foreach (var platform in platforms)
            {
                sb.AppendLine($"{platform.name} [id = {platform.id}]");
            }
            await _cc.Reply(Context,sb.ToString());
        }

        [Command("rlstats", RunMode = RunMode.Async)]
        [Summary("Get Rocket League Stats. Use this command with set, followed by your steam URL/ID/VanityName to set a default user (rlstats set URL/ID/VanityName")]
        public async Task RlStats([Remainder]string input = "")
        {      
            System.Console.WriteLine("RLSTATS");      
            StringBuilder sb = new StringBuilder();
            var rlStats = new RlStat();
            bool ps = false;
            if (!string.IsNullOrEmpty(input))
            {
                if (input.Split(' ').Count() > 1)
                {
                    string arg = input.Split(' ')[1].ToLower().ToString();
                    switch (input.Split(' ')[0].ToString().ToLower())
                    {
                        case "get":
                            {
                                if (!string.IsNullOrEmpty(arg))
                                {
                                    if (input.Split(' ').Count() > 2 && input.Split(' ')[2].ToLower() == "ps")
                                    {
                                        await GetStats(arg, true);
                                        ps = true;
                                    }
                                    else
                                    {
                                        await GetStats(arg);
                                    }
                                }
                                else
                                {
                                    await _cc.Reply(Context, "Please specify a steam ID/vanity name to get the stats of!");
                                }
                                break;
                            }
                        case "set":
                            {
                                await SetStats(arg);
                                break;
                            }
                        case "help":
                            {

                                break;
                            }
                    }
                }
                else
                {
                    sb.AppendLine($"Please specify a name / steamID after using the set/get commands!");
                    await _cc.Reply(Context, sb.ToString());
                    
                    return;
                }
            }
            else
            {
                await SendStats(ps);
                //await Context.Message.AddReactionAsync("😁");
            }
        }

        public async Task SetStats(string name)
        {
            try
            {
                using (var db = new NinjaBotEntities())
                {
                    string channel = Context.Channel.Name;
                    string userName = Context.User.Username;                   
                    StringBuilder sb = new StringBuilder();
                    string rlUserName = name;

                    if (Uri.IsWellFormedUriString(rlUserName, UriKind.RelativeOrAbsolute))
                    {
                        rlUserName = rlUserName.TrimEnd('/');
                        rlUserName = rlUserName.Substring(rlUserName.LastIndexOf('/') + 1);
                    }

                    SteamModel.Player fromSteam = _steam.getSteamPlayerInfo(rlUserName);
                    if (string.IsNullOrEmpty(fromSteam.steamid))
                    {
                        sb.AppendLine($"{Context.User.Mention}, Please specify a steam username/full profile URL to link with your Discord username!");
                        await _cc.Reply(Context, sb.ToString());
                        return;
                    }
                    try
                    {
                        var addUser = new RlStat();
                        var rlUser = db.RlStats.Where(r => r.DiscordUserName == userName).FirstOrDefault();
                        if (rlUser == null)
                        {
                            addUser.DiscordUserName = userName;
                            addUser.SteamID = long.Parse(fromSteam.steamid);
                            addUser.DiscordUserID = (long)Context.User.Id;
                            db.RlStats.Add(addUser);
                        }
                        else
                        {
                            rlUser.SteamID = long.Parse(fromSteam.steamid);
                            rlUser.DiscordUserID = (long)Context.User.Id;
                            //rl.setUserName(userName, fromSteam.steamid);
                        }
                        db.SaveChanges();
                        sb.AppendLine($"{Context.User.Mention}, you've associated [**{fromSteam.personaname}**] with your Discord name!");
                        await _cc.Reply(Context, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"RL Stats: Error setting name -> {ex.Message}");
                        sb.AppendLine($"{Context.User.Mention}, something went wrong, sorry :(");
                        await _cc.Reply(Context, sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        public async Task GetStats(string name)
        {
            StringBuilder sb = new StringBuilder();
            SteamModel.Player fromSteam = _steam.getSteamPlayerInfo(name);

            if (string.IsNullOrEmpty(fromSteam.steamid))
            {
                sb.AppendLine($"Unable to find steam user for steam name/id: {name}!");                
                await _cc.Reply(Context, sb.ToString());
                return;
            }
            else
            {                
                try
                {                    
                    EmbedBuilder embed = await rlEmbed(sb, fromSteam);
                    await _cc.Reply(Context, embed);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }                                                
                return;
            }
        }

        public async Task GetStats(string name, bool ps)
        {
                try
                {
                    StringBuilder sb = new StringBuilder();                    
                    EmbedBuilder embed = await rlEmbed(sb, name);
                    await _cc.Reply(Context, embed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return;            
        }

        public async Task SendStats(bool ps)
        {
            try
            {
                string userName = Context.User.Username;
                string channel = Context.Channel.Name;
                StringBuilder sb = new StringBuilder();
                RlStat rlUser = null;
                using (var db = new NinjaBotEntities())
                {
                    rlUser = db.RlStats.FirstOrDefault(r => r.DiscordUserName == userName);
                }
                if (rlUser == null)
                {
                    sb.AppendLine($"Unable to find steam name association for discord user {userName}");
                    await _cc.Reply(Context, sb.ToString());                    
                    return;
                }
                else
                {
                    string steamUserId = rlUser.SteamID.ToString();
                    SteamModel.Player fromSteam = _steam.getSteamPlayerInfo(steamUserId);

                    if (string.IsNullOrEmpty(fromSteam.steamid))
                    {
                        sb.AppendLine($"Unable to find steam user for steam name/id: {steamUserId}!");
                        sb.AppendLine($"Please try using !rlstats set steamVanityUserNameOrID");                        
                        await _cc.Reply(Context, sb.ToString());
                        return;
                    }
                    else
                    {
                        
                        //sb.AppendLine($"Stats from: {fullURL}");
                        EmbedBuilder embed = await rlEmbed(sb, fromSteam);
                        
                        await _cc.Reply(Context, embed);
                    }
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error looking up stats {ex.Message}");
            }
        }
        
        private async Task<EmbedBuilder> rlEmbed(StringBuilder sb, SteamModel.Player fromSteam)
        {
            RlUserStat getStats = null;
            List<RocketLeagueStats> stats = null;
            var embed = new EmbedBuilder();
            try
            {
                stats = await _rl.getRLStats(fromSteam.steamid);
                getStats = await GetStatsFromDb(fromSteam);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error getting RL Stats -> [{ex.Message}]");
                embed.Title = $"Error getting stats for {fromSteam.personaname}!";
                embed.Description = $"Sorry, something dun went wrong :(";
                return embed;                
            }
            if (stats != null)
            {
                await InsertStats(stats, fromSteam);
            }
            foreach (var stat in stats)
            {
                string rankMoji = ":wavy_dash:";
                if (getStats != null)
                {
                    switch (stat.Title)
                    {
                        case "Doubles 2v2":
                            {
                                var doubles = getStats.Ranked2v2;
                                if (doubles != null)
                                {
                                    if (int.Parse(doubles) < stat.MMR)
                                    {
                                        rankMoji = $":arrow_up: (up **{(stat.MMR - int.Parse(doubles)).ToString()}**)";
                                    }
                                    else if (int.Parse(doubles) > stat.MMR)
                                    {
                                        rankMoji = $":arrow_down: (down **{(int.Parse(doubles) - stat.MMR).ToString()}**)";
                                    }
                                }
                                break;
                            }
                        case "Duel 1v1":
                            {
                                var duel = getStats.RankedDuel;
                                if (duel != null)
                                {
                                    if (int.Parse(duel) < stat.MMR)
                                    {
                                        rankMoji = $":arrow_up: (up **{(stat.MMR - int.Parse(duel)).ToString()}**)";
                                    }
                                    else if (int.Parse(duel) > stat.MMR)
                                    {
                                        rankMoji = $":arrow_down: (down **{(int.Parse(duel) - stat.MMR).ToString()}**)";
                                    }
                                }
                                break;
                            }
                        case "Solo Standard 3v3":
                            {
                                var standard = getStats.RankedSolo;
                                if (standard != null)
                                {
                                    if (int.Parse(standard) < stat.MMR)
                                    {
                                        rankMoji = $":arrow_up: (up **{(stat.MMR - int.Parse(standard)).ToString()}**)";
                                    }
                                    else if (int.Parse(standard) > stat.MMR)
                                    {
                                        rankMoji = $":arrow_down: (down **{(int.Parse(standard) - stat.MMR).ToString()}**)";
                                    }
                                }
                                break;
                            }
                        case "Standard 3v3":
                            {
                                var threev = getStats.Ranked3v3;
                                if (threev != null)
                                {
                                    if (int.Parse(threev) < stat.MMR)
                                    {
                                        rankMoji = $":arrow_up: (up **{(stat.MMR - int.Parse(threev)).ToString()}**)";
                                    }
                                    else if (int.Parse(threev) > stat.MMR)
                                    {
                                        rankMoji = $":arrow_down: (down **{(int.Parse(threev) - stat.MMR).ToString()}**)";
                                    }
                                }
                                break;
                            }
                        case "Unranked":
                            {
                                var unranked = getStats.Unranked;
                                if (unranked != null)
                                {
                                    if (int.Parse(unranked) < stat.MMR)
                                    {
                                        rankMoji = $":arrow_up: (up **{(stat.MMR - int.Parse(unranked)).ToString()}**)";
                                    }
                                    else if (int.Parse(unranked) > stat.MMR)
                                    {
                                        rankMoji = $":arrow_down: (down **{(int.Parse(unranked) - stat.MMR).ToString()}**)";
                                    }
                                }
                                break;
                            }
                    }
                }
                sb.AppendLine($"__{stat.Title}__");
                //sb.AppendLine($"Games Played: **{stat.GamesPlayed}**");
                sb.AppendLine($"Rank: **{stat.Rank}**(Div **{stat.Division}**)");
                sb.AppendLine($"MMR: **{stat.MMR}** {rankMoji}");
                //sb.AppendLine($"**{stat.Percentage}**");
                sb.AppendLine($"");
            }
            var statsUrl = stats.Where(s => s.FromURL != null).Select(s => s.FromURL).FirstOrDefault();
            if (statsUrl != null)
            {
                sb.AppendLine($"Stats from: {statsUrl}");
            }            
            embed.WithColor(new Color(0, 71, 171));            
            embed.ThumbnailUrl = fromSteam.avatarfull;
            //embed.ThumbnailUrl = Context.User.GetAvatarUrl();
            embed.Title = $"__Rocket League Stats For [**{fromSteam.personaname}**]__";
            embed.Description = sb.ToString();
            return embed;
        }

        private async Task<EmbedBuilder> rlEmbed(StringBuilder sb, string psName)
        {
            List<RocketLeagueStats> stats = null;
            try
            {
                stats = await _rl.getRLStats(psName, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting RL Stats -> [{ex.Message}]");
            }

            foreach (var stat in stats)
            {
                sb.AppendLine($"__{stat.Title}__");
                //sb.AppendLine($"Games Played: **{stat.GamesPlayed}**");
                sb.AppendLine($"Rank: **{stat.Rank}**(Div **{stat.Division}**)");
                sb.AppendLine($"MMR: **{stat.MMR}**");
                //sb.AppendLine($"**{stat.Percentage}**");
                sb.AppendLine($"");
            }
            var statsUrl = stats.Where(s => s.FromURL != null).Select(s => s.FromURL).FirstOrDefault();
            if (statsUrl != null)
            {
                sb.AppendLine($"Stats from: {statsUrl}");
            }
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0, 71, 171));
            embed.ThumbnailUrl = Context.User.GetAvatarUrl();
            //embed.ThumbnailUrl = Context.User.GetAvatarUrl();
            embed.Title = $"__Rocket League Stats For [**{psName}**]__";
            embed.Description = sb.ToString();
            return embed;
        }

        private async Task InsertStats(List<RocketLeagueStats> stats, SteamModel.Player fromSteam)
        {
            long steamId = long.Parse(fromSteam.steamid);
            using (var db = new NinjaBotEntities())
            {
                var statAdd = new RlUserStat();
                var getStat = db.RlUserStats.Where(s => s.SteamID == steamId).FirstOrDefault();
                var doubles = stats.Where(s => s.Title == "Doubles 2v2").FirstOrDefault();
                var duel = stats.Where(s => s.Title == "Duel 1v1").FirstOrDefault();
                var soloStandard = stats.Where(s => s.Title == "Solo Standard 3v3").FirstOrDefault();
                var rankedThrees = stats.Where(s => s.Title == "Standard 3v3").FirstOrDefault();
                var unranked = stats.Where(s => s.Title == "Unranked").FirstOrDefault();
               
                if (getStat == null)
                {                    
                    if (doubles != null)
                    {
                        statAdd.Ranked2v2 = doubles.MMR.ToString();
                    }
                    else
                    {
                        statAdd.Ranked2v2 = "0";
                    }                    
                    if (duel != null)
                    {
                        statAdd.RankedDuel = duel.MMR.ToString();
                    }
                    else
                    {
                        statAdd.RankedDuel = "0";
                    }                    
                    if (soloStandard != null)
                    {
                        statAdd.RankedSolo = soloStandard.MMR.ToString();
                    }
                    else
                    {
                        statAdd.RankedSolo = "0";
                    }                    
                    if (rankedThrees != null)
                    {
                        statAdd.Ranked3v3 = rankedThrees.MMR.ToString();
                    }
                    else
                    {
                        statAdd.Ranked3v3 = "0";
                    }                    
                    if (unranked != null)
                    {
                        statAdd.Unranked = unranked.MMR.ToString();
                    }
                    else
                    {
                        statAdd.Unranked = "0";
                    }              
                    statAdd.SteamID = steamId;
                    db.RlUserStats.Add(statAdd);
                }
                else
                {                    
                    if (doubles != null)
                    {
                        getStat.Ranked2v2 = doubles.MMR.ToString();
                    }
                 
                    if (duel != null)
                    {
                        getStat.RankedDuel = duel.MMR.ToString();
                    }
                 
                    if (soloStandard != null)
                    {
                        getStat.RankedSolo = soloStandard.MMR.ToString();
                    }
                 
                    if (rankedThrees != null)
                    {
                        getStat.Ranked3v3 = rankedThrees.MMR.ToString();
                    }
                 
                    if (unranked != null)
                    {
                        getStat.Unranked = unranked.MMR.ToString();
                    }                    
                    getStat.SteamID = steamId;
                }
                await db.SaveChangesAsync();
            }
        }

        private async Task<RlUserStat> GetStatsFromDb(SteamModel.Player fromSteam)
        {
            RlUserStat rlUserStats = null;
            long steamId = long.Parse(fromSteam.steamid);
            using (var db = new NinjaBotEntities())
            {
                rlUserStats = db.RlUserStats.Where(r => r.SteamID == steamId).FirstOrDefault();
            }
            return rlUserStats;
        }
    }
}