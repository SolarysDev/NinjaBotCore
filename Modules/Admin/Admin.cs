using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinjaBotCore.Database;

namespace NinjaBotCore.Modules.Admin
{
    public class Admin : ModuleBase
    {
        private static DiscordSocketClient _client;
        private static ChannelCheck _cc;
        public Admin(DiscordSocketClient client, ChannelCheck cc)
        {
            if (_client == null)
            {
                _client = client;
            }
            if (_cc == null)
            {
                _cc = cc;
            }
            Console.WriteLine($"Admin module loaded");
        }

        [Command("kick", RunMode = RunMode.Async)]
        [Summary("Kick someone, not nice... but needed sometimes")]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task KickUser(IGuildUser user, [Remainder] string reason = null)
        {
            var embed = new EmbedBuilder();
            embed.ThumbnailUrl = user.GetAvatarUrl();
            StringBuilder sb = new StringBuilder();
            try
            {
                await user.KickAsync();
                embed.Title = $"Kicking {user.Username}";
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "Buh bye.";
                }
                sb.AppendLine($"Reason: [**{reason}**]");
            }
            catch (Exception ex)
            {
                embed.Title = $"Error attempting to kick {user.Username}";
                sb.AppendLine($"[{ex.Message}]");
            }
            embed.Description = sb.ToString();
            embed.WithColor(new Color(0, 0, 255));
            await _cc.Reply(Context, embed);
        }

        [Command("ban", RunMode = RunMode.Async)]
        [Summary("Ban someone, not nice... but needed sometimes")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task BanUser(IGuildUser user, [Remainder] string reason = null)
        {
            var embed = new EmbedBuilder();
            embed.ThumbnailUrl = user.GetAvatarUrl();
            StringBuilder sb = new StringBuilder();
            try
            {
                await Context.Guild.AddBanAsync(user);
                embed.Title = $"Banning {user.Username}";
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "Buh bye.";
                }
                sb.AppendLine($"Reason: [**{reason}**]");
            }
            catch (Exception ex)
            {
                embed.Title = $"Error attempting to ban {user.Username}";
                sb.AppendLine($"[{ex.Message}]");
            }
            embed.Description = sb.ToString();
            embed.WithColor(new Color(0, 0, 255));
            await _cc.Reply(Context, embed);
        }

        [Command("unban", RunMode = RunMode.Async)]
        [Summary("Unban someone... whew!")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task UnBanUser(string user, [Remainder] string reason = null)
        {
            var embed = new EmbedBuilder();
            embed.ThumbnailUrl = Context.Guild.IconUrl;
            StringBuilder sb = new StringBuilder();
            var currentBans = await Context.Guild.GetBansAsync();
            var bannedUser = currentBans.Where(c => c.User.Username.Contains(user)).FirstOrDefault();
            try
            {
                await Context.Guild.RemoveBanAsync(bannedUser.User.Id);
                embed.Title = $"UnBanning {bannedUser.User.Username}";
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "Hello again.";
                }
                sb.AppendLine($"Reason: [**{reason}**]");
            }
            catch (Exception ex)
            {
                embed.Title = $"Error attempting to unban {bannedUser.User.Username}";
                sb.AppendLine($"[{ex.Message}]");
            }
            embed.Description = sb.ToString();
            embed.WithColor(new Color(0, 0, 255));
            await _cc.Reply(Context, embed);
        }

        [Command("list-bans", RunMode = RunMode.Async)]
        [Summary("List the users currently banned on the server")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task ListBans()
        {
            var embed = new EmbedBuilder();
            embed.ThumbnailUrl = Context.Guild.IconUrl;
            StringBuilder sb = new StringBuilder();
            try
            {
                embed.Title = $"User bans on {Context.Guild.Name}";
                var bans = await Context.Guild.GetBansAsync();
                if (bans.Count > 0)
                {
                    foreach (var ban in bans)
                    {
                        sb.AppendLine($":black_medium_small_square: **{ban.User.Username}** (*{ban.Reason}*)");
                    }
                }
                else
                {
                    sb.AppendLine($"Much empty, such space!");
                }

            }
            catch (Exception ex)
            {
                embed.Title = $"Error attempting to list bans for **{Context.Guild.Name}**";
                sb.AppendLine($"[{ex.Message}]");
            }
            embed.Description = sb.ToString();
            embed.WithColor(new Color(0, 0, 255));
            await _cc.Reply(Context, embed);
        }

        [Command("set-join-message", RunMode = RunMode.Async)]
        [Alias("set-join")]
        [Summary("Change the greeting message for when someone joins the server")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task ChangeGreeting([Remainder] string args = null)
        {
            var embed = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(args))
            {
                embed.Title = $"Join greeting change for {Context.Guild.Name}";
                sb.AppendLine("New message:");
                sb.AppendLine(args);
                using (var db = new NinjaBotEntities())
                {
                    try
                    {
                        var guildGreetingInfo = db.ServerGreetings.Where(g => g.DiscordGuildId == (long)Context.Guild.Id).FirstOrDefault();
                        if (guildGreetingInfo != null)
                        {
                            guildGreetingInfo.Greeting = args.Trim();
                            guildGreetingInfo.SetById = (long)Context.User.Id;
                            guildGreetingInfo.SetByName = Context.User.Username;
                            guildGreetingInfo.TimeSet = DateTime.Now;
                        }
                        else
                        {
                            db.ServerGreetings.Add(new ServerGreeting
                            {
                                DiscordGuildId = (long)Context.Guild.Id,
                                Greeting = args.Trim(),
                                SetById = (long)Context.User.Id,
                                SetByName = Context.User.Username,
                                TimeSet = DateTime.Now
                            });
                        }
                        await db.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        embed.Title = $"Error changing message";
                        sb.AppendLine($"{Context.User.Mention},");
                        sb.AppendLine($"I've encounted an error, please contact the owner for help.");
                    }
                }
            }
            else
            {
                embed.Title = $"Error changing message";
                sb.AppendLine($"{Context.User.Mention},");
                sb.AppendLine($"Please provided a message!");
            }
            embed.Description = sb.ToString();
            embed.WithColor(new Color(0, 255, 0));
            embed.ThumbnailUrl = Context.Guild.IconUrl;
            await _cc.Reply(Context, embed);
        }

        [Command("set-part-message", RunMode = RunMode.Async)]
        [Alias("set-part")]
        [Summary("Change the message displayed when someone leaves the server")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task ChangeParting([Remainder] string args = null)
        {
            var embed = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(args))
            {
                embed.Title = $"Parting message change for {Context.Guild.Name}";
                sb.AppendLine("New message:");
                sb.AppendLine(args);
                using (var db = new NinjaBotEntities())
                {
                    try
                    {
                        var guildGreetingInfo = db.ServerGreetings.Where(g => g.DiscordGuildId == (long)Context.Guild.Id).FirstOrDefault();
                        if (guildGreetingInfo != null)
                        {
                            guildGreetingInfo.PartingMessage = args.Trim();
                            guildGreetingInfo.SetById = (long)Context.User.Id;
                            guildGreetingInfo.SetByName = Context.User.Username;
                            guildGreetingInfo.TimeSet = DateTime.Now;
                        }
                        else
                        {
                            db.ServerGreetings.Add(new ServerGreeting
                            {
                                DiscordGuildId = (long)Context.Guild.Id,
                                PartingMessage = args.Trim(),
                                SetById = (long)Context.User.Id,
                                SetByName = Context.User.Username,
                                TimeSet = DateTime.Now
                            });
                        }
                        await db.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        embed.Title = $"Error changing message";
                        sb.AppendLine($"{Context.User.Mention},");
                        sb.AppendLine($"I've encounted an error, please contact the owner for help.");
                    }
                }
            }
            else
            {
                embed.Title = $"Error changing message";
                sb.AppendLine($"{Context.User.Mention},");
                sb.AppendLine($"Please provided a message!");
            }
            embed.Description = sb.ToString();
            embed.WithColor(new Color(0, 255, 0));
            embed.ThumbnailUrl = Context.Guild.IconUrl;
            await _cc.Reply(Context, embed);
        }

        [Command("toggle-greetings", RunMode = RunMode.Async)]
        [Summary("Toogle greeting users that join/leave this server")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task ToggleGreetings()
        {
            var embed = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            using (var db = new NinjaBotEntities())
            {
                try
                {
                    var currentSetting = db.ServerGreetings.Where(g => g.DiscordGuildId == (long)Context.Guild.Id).FirstOrDefault();
                    if (currentSetting != null)
                    {
                        if (currentSetting.GreetUsers == true)
                        {
                            currentSetting.GreetUsers = false;
                            sb.AppendLine("Greetings have been disabled!");
                        }
                        else
                        {
                            currentSetting.GreetUsers = true;
                            sb.AppendLine("Greetings have been enabled!");
                        }
                    }
                    else
                    {
                        db.ServerGreetings.Add(new ServerGreeting
                        {
                            DiscordGuildId = (long)Context.Guild.Id,
                            GreetUsers = true
                        });
                        sb.AppendLine("Greetings have been enabled!");
                    }
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error toggling greetings -> [{ex.Message}]!");
                }
            }
            embed.Title = $"User greeting settings for {Context.Guild.Name}";
            embed.Description = sb.ToString();
            embed.WithColor(new Color(0, 255, 0));
            embed.ThumbnailUrl = Context.Guild.IconUrl;
            await _cc.Reply(Context, embed);
        }

        [Command("blacklist", RunMode = RunMode.Async)]
        [Summary("Blacklist someone (must be bot owner)")]
        [RequireOwner]
        public async Task BlackList(IGuildUser user, [Remainder] string reason = null)
        {
            var embed = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                using (var db = new NinjaBotEntities())
                {
                    var blacklist = db.Blacklist;
                    if (blacklist != null)
                    {
                        var getUser = blacklist.Where(b => b.DiscordUserId == (long)user.Id).FirstOrDefault();
                        if (getUser != null)
                        {
                            sb.AppendLine($"Unblacklisting {user.Username}");
                            blacklist.Remove(getUser);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(reason))
                            {
                                reason = "just because";
                            }
                            blacklist.Add(new Blacklist
                            {
                                DiscordUserId = (long)user.Id,
                                DiscordUserName = user.Username,
                                Reason = reason,
                                WhenBlacklisted = DateTime.Now
                            });
                            sb.AppendLine($"Blacklisting [**{user.Username}**] -> [*{reason}*]");
                        }
                        embed.Title = "[Blacklist]";
                        embed.Description = sb.ToString();
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error attempting to blacklist [{user.Username}] -> [{ex.Message}]");
            }
            await _cc.Reply(Context, embed);
        }

        [Command("clear", RunMode = RunMode.Async)]
        [Summary("Clear an amount of messages in the channel")]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearMessage([Remainder] int numberOfMessages = 5)
        {
            if (numberOfMessages > 25)
            {
                numberOfMessages = 25;
            }
            var messagesToDelete = await Context.Channel.GetMessagesAsync(numberOfMessages).Flatten();
            await Context.Channel.DeleteMessagesAsync(messagesToDelete);
        }

        [Command("set-note", RunMode = RunMode.Async)]
        [Alias("snote")]
        [Summary("Set a note associated with a discord server")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetNote([Remainder] string note)
        {
            string result = await SetNoteInfo(Context, note);
            var embed = new EmbedBuilder();
            embed.Title = $":notepad_spiral:Notes for {Context.Guild.Name}:notepad_spiral:";
            embed.Description = result;
            embed.ThumbnailUrl = Context.Guild.IconUrl;
            embed.WithColor(new Color(0, 255, 0));
            await _cc.Reply(Context, embed);
        }

        [Command("get-note", RunMode = RunMode.Async)]
        [Alias("note")]
        [Summary("Get a note associated with a discord server")]
        public async Task GetNote()
        {
            string note = await GetNoteInfo(Context);
            var embed = new EmbedBuilder();
            embed.Title = $":notepad_spiral:Notes for {Context.Guild.Name}:notepad_spiral:";
            embed.ThumbnailUrl = Context.Guild.IconUrl;
            embed.Description = note;
            embed.WithColor(new Color(0, 255, 0));
            await _cc.Reply(Context, embed);
        }

        private async Task<string> SetNoteInfo(ICommandContext Context, string noteText)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (var db = new NinjaBotEntities())
                {
                    var currentNote = db.Notes.FirstOrDefault(c => c.ServerId == (long)Context.Guild.Id);
                    if (currentNote == null)
                    {
                        Note n = new Note()
                        {
                            Note1 = noteText,
                            ServerId = (long)Context.Guild.Id,
                            ServerName = Context.Guild.Name,
                            SetBy = Context.User.Username,
                            SetById = (long)Context.User.Id,
                            TimeSet = DateTime.Now
                        };
                        db.Notes.Add(n);
                    }
                    else
                    {
                        currentNote.Note1 = noteText;
                        currentNote.SetBy = Context.User.Username;
                        currentNote.SetById = (long)Context.User.Id;
                        currentNote.TimeSet = DateTime.Now;
                    }
                    await db.SaveChangesAsync();
                }
                sb.AppendLine($"Note successfully added for server [**{Context.Guild.Name}**] by [**{Context.User.Username}**]!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting note {ex.Message}");
                sb.AppendLine($"Something went wrong adding a note for server [**{Context.Guild.Name}**] :(");
            }
            return sb.ToString();
        }

        private async Task<string> GetNoteInfo(ICommandContext Context)
        {
            StringBuilder sb = new StringBuilder();
            using (var db = new NinjaBotEntities())
            {
                var note = db.Notes.FirstOrDefault(n => n.ServerId == (long)Context.Guild.Id);
                if (note == null)
                {
                    sb.AppendLine($"Unable to find a note for server [{Context.Guild.Name}], perhaps try adding one by using {NinjaBot.Prefix}set-note \"Note goes here!\"");
                }
                else
                {
                    sb.AppendLine(note.Note1);
                    sb.AppendLine();
                    sb.Append($"*Note set by [**{note.SetBy}**] on [**{note.TimeSet}**]*");
                }
            }
            return sb.ToString();
        }
        //[Command("Deafen")]
    }
}