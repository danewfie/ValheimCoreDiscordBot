using Discord;
using Discord.WebSocket;
using Medallion.Shell;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ValheimCoreDiscordBot.Services;

namespace ValheimCoreDiscordBot.Valheim
{
    public class ValheimServerUtilities
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        public ObservableCollection<string> updateLog;
        private ulong ChannelToken;
        private static string SteamAPIKey;

        public ValheimServerUtilities(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _config = services.GetRequiredService<IConfiguration>();
            _logger = services.GetRequiredService<ILogger<CommandHandler>>();
            _services = services;
            ChannelToken = ulong.Parse(_config["Channel"]);
            SteamAPIKey = _config["SteamAPIKey"];

            updateLog = new ObservableCollection<string>();
            updateLog.CollectionChanged += UpdateLog_CollectionChanged;
        }

        private void UpdateLog_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    // post some output
                    foreach (var item in e.NewItems)
                    {
                        SendMessage(item.ToString());
                    }
                    
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
        }

        private async Task SendMessage(string message)
        {
            var channel = _client.GetChannel(ChannelToken) as ISocketMessageChannel;
            await channel.SendMessageAsync(message.ToString());
            _logger.LogInformation(message);
        }

        private async Task SendEmbedMessage(EmbedBuilder embedBuilder)
        {
            var channel = _client.GetChannel(ChannelToken) as ISocketMessageChannel;
            await channel.SendMessageAsync("", false, embedBuilder.Build());
            //_logger.LogInformation(message);
        }

        public static async Task<SteamRoot> GetSteamInfo(string id)
        {
            using (HttpClient client = new HttpClient())
            {
                var streamTask = client.GetStreamAsync($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={SteamAPIKey}&steamids={id}");

                return await JsonSerializer.DeserializeAsync<SteamRoot>(await streamTask);
            }
        }

        public static string _Log_RecoveryInfo { get { return Environment.CurrentDirectory + "/RecoveryInfo.log"; } }

        public static void CheckLogFiles()
        {
            if (!File.Exists(_Log_RecoveryInfo))
            {
                File.WriteAllText(_Log_RecoveryInfo, "");
            }
        }

        internal static void SaveProcessID(string pid)
        {
            File.WriteAllText(_Log_RecoveryInfo, pid);
        }

        internal static string GetProcessID()
        {
            return File.ReadAllText(_Log_RecoveryInfo);
        }

        internal static void ClearProcessID()
        {
            File.WriteAllText(_Log_RecoveryInfo, "");
        }

        internal async Task UpdateServer()
        {
            var bat = GenerateValheimUpdateBat(_config["steamcmdDir"]);
            Command cmd = Command.Run("cmd.exe", new[] { "/c ", bat }, options => options.StartInfo(psi => psi.WorkingDirectory = _config["steamcmdDir"]));
            List<string> output = new List<string>();
            try
            {
                cmd.RedirectTo(output);
                cmd.Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            finally
            {
                cmd.Kill();
                output.Add("--Update Command Completed-- Please start server if no errors found!");
                var ChannelToken = ulong.Parse(_config["Channel"]);
                StringBuilder message = new StringBuilder();
                foreach (var item in output)
                {
                    message.AppendLine(item);
                    _logger.LogInformation(item);
                }

                var channel = _client.GetChannel(ChannelToken) as ISocketMessageChannel;
                await channel.SendMessageAsync(message.ToString());
            }
        }

        private static string GenerateValheimUpdateBat(string dir)
        {
            string filePath = dir + @"\valheim_server_update.bat";
            File.WriteAllText(filePath, "steamcmd +login anonymous +app_update 896660 +exit");
            return filePath;
        }
    }

    public enum ServerStatus
    {
        Starting,
        Terminating,
        Running,
        Recovered,
        Offline
    }
}
