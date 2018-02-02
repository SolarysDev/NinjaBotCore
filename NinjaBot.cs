using Discord.Net;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using NinjaBotCore.Database;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using NinjaBotCore.Modules.Wow;
using NinjaBotCore.Modules.Admin;
using NinjaBotCore.Modules.Steam;
using NinjaBotCore.Modules.RocketLeague;
using NinjaBotCore.Modules.Fun;
using NinjaBotCore.Modules.Away;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NinjaBotCore.Services;
using NinjaBotCore.Modules.Giphy;
using NinjaBotCore.Modules.Weather;
using NinjaBotCore.Modules.YouTube;
using Microsoft.EntityFrameworkCore;

namespace NinjaBotCore
{
    public class NinjaBot
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;        
        public static DiscordSocketClient Client;
        private IConfigurationRoot _config;

        public async Task StartAsync()
        {            
            //Create the configuration
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");            
            _config = _builder.Build();
            
            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    //LogLevel = LogSeverity.Debug,
                    LogLevel = LogSeverity.Verbose, 
                    MessageCacheSize = 1000
                }))
                .AddSingleton(_config)
                .AddSingleton(new CommandService(new CommandServiceConfig 
                { 
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Verbose,
                    CaseSensitiveCommands = false, 
                    ThrowOnError = false 
                }))
                .AddSingleton<LoggingService>()
                .AddSingleton<WowApi>()
                .AddSingleton<WarcraftLogs>()
                .AddSingleton<ChannelCheck>()
                .AddSingleton<RocketLeague>()
                .AddSingleton<OxfordApi>()
                .AddSingleton<AwayCommands>()
                .AddSingleton<RlStatsApi>()
                .AddSingleton<UserInteraction>()
                .AddSingleton<CommandHandler>()
                .AddLogging()
                .AddSingleton<StartupService>()
                .AddSingleton<SteamApi>()        
                .AddSingleton<GiphyApi>()    
                .AddSingleton<WeatherApi>()
                .AddSingleton<YouTubeApi>();

            var serviceProvider = services.BuildServiceProvider();
                          
            //Start the bot
            await serviceProvider.GetRequiredService<StartupService>().StartAsync();        
            //Start the command handler
            serviceProvider.GetRequiredService<CommandHandler>();
            
            //Instantiate services    
            serviceProvider.GetRequiredService<LoggingService>();       
            serviceProvider.GetRequiredService<UserInteraction>();              
            serviceProvider.GetRequiredService<AwayCommands>();
            //serviceProvider.GetRequiredService<WowApi>();
            //serviceProvider.GetRequiredService<WarcraftLogs>();
            //serviceProvider.GetRequiredService<RlStatsApi>();                        
            //serviceProvider.GetRequiredService<OxfordApi>();
            //serviceProvider.GetRequiredService<ChannelCheck>();
            //serviceProvider.GetRequiredService<RocketLeague>();
            //serviceProvider.GetRequiredService<SteamApi>();
            //serviceProvider.GetRequiredService<GiphyApi>();
            //serviceProvider.GetRequiredService<WeatherApi>();
            //serviceProvider.GetRequiredService<YouTubeApi>();

            //var logger = serviceProvider.GetRequiredService<LoggingService>();
            //logger.DoAction("test for the Ninja");
            //serviceProvider.GetRequiredService<DiscordSocketClient>().Log += Log;
            //_client.Log += Log;            
            //await _client.LoginAsync(TokenType.Bot, Config.Token);
            //await _client.StartAsync();
            //await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);
            //await services.GetRequiredService<TagService>().InitializeAsync(services);                               
            //Client = serviceProvider.GetService<DiscordSocketClient>();             
            // Block this program until it is closed.                     

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}