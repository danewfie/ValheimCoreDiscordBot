using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using ValheimCoreDiscordBot.Valheim;

namespace ValheimCoreDiscordBot.Services
{
    public class ServerService
    {
        public string MyStatus { get; set; }
        public DateTime dateTime { get; set; }
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private ValheimServer _server;
        private ulong ChannelToken { get; set; }

        public ServerService(IServiceProvider services)
        {
            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _client = services.GetRequiredService<DiscordSocketClient>();
            _config = services.GetRequiredService<IConfiguration>();
            _logger = services.GetRequiredService<ILogger<CommandHandler>>();
            _services = services;

            try
            {
                ChannelToken = ulong.Parse(_config["Channel"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            _server = new ValheimServer(_services);
            _server.output.CollectionChanged += Output_CollectionChanged;
        }

        internal void Update()
        {
            // check if updates are ok
            // safe shutdowns etc;

            // Generate Update
            //ValheimServerUtilities.UpdateServer(_config, _logger);
            ValheimServerUtilities vsu = new ValheimServerUtilities(_services);
            vsu.UpdateServer();
            // restart server maybe
        }

        private void Output_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    // create logic here
                    var newitems = e.NewItems;
                    foreach (var item in newitems)
                    {
                        var value = item.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (value.StartsWith("~|"))
                            {
                                // set server status
                                SetGameState();
                            }
                            else
                                ProcessOutput(value);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    // cleaning up old data .. do nothing
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

        private void SetGameState()
        {
            // sets playing message under bots name
            _client.SetGameAsync(_server.GetGameState());
        }

        private void ProcessOutput(string message)
        {
            // sends message to specific channel
            var channel = _client.GetChannel(ChannelToken) as ISocketMessageChannel;
            channel.SendMessageAsync(message);
        }

        public string Start()
        {
            // starts server
            _server.StartServer();
            SetGameState();
            return "Starting";
        }

        public string Stop()
        {
            // stops server
            _server.KillServer();
            SetGameState();
            return "Terminating";
        }

        public EmbedBuilder GetStatus()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle("Server Details");
            embedBuilder.AddField("Status", _server.Server_Details.Server_Status);

            switch (_server.Server_Details.Server_Status)
            {
                case ServerStatus.Starting:
                    embedBuilder.WithColor(Color.Blue);
                    break;
                case ServerStatus.Terminating:
                    embedBuilder.WithColor(Color.Red);
                    break;
                case ServerStatus.Running:
                    embedBuilder.WithColor(Color.Green);
                    embedBuilder.AddField("Uptime", getRuntime());
                    break;
                case ServerStatus.Recovered:
                case ServerStatus.Offline:
                default:
                    break;
            }
            return embedBuilder;
        }

        public EmbedBuilder GetServerInfo()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle($"Valheim Server ({_server.Server_Details.Server_Status})");
            embedBuilder.AddField($"{GetPublicIP()}:{_config["Port"]}", "IP:Port");
            embedBuilder.AddField(_config["Password"], "Password");

            switch (_server.Server_Details.Server_Status)
            {
                case ServerStatus.Starting:
                    embedBuilder.WithColor(Color.Blue);
                    break;
                case ServerStatus.Terminating:
                    embedBuilder.WithColor(Color.Red);
                    break;
                case ServerStatus.Running:
                    embedBuilder.WithColor(Color.Green);
                    embedBuilder.AddField("Uptime", getRuntime());
                    break;
                case ServerStatus.Recovered:
                case ServerStatus.Offline:
                default:
                    break;
            }
            return embedBuilder;
        }

        private string getRuntime()
        {
            var days = _server.Server_Details.Server_Runtime.Days;
            var hours = _server.Server_Details.Server_Runtime.Hours;
            var min = _server.Server_Details.Server_Runtime.Minutes;
            var sec = _server.Server_Details.Server_Runtime.Seconds;
            if (days > 0)
            {
                return $"{days} Days, {hours} Hours, {min} Minutes, {sec} Seconds";
            }
            else if (hours > 0)
            {
                return $"{hours} Hours, {min} Minutes, {sec} Seconds";
            }
            else if (min > 0)
            {
                return $"{min} Minutes, {sec} Seconds";
            }
            else
                return $"{sec} Seconds";
        }

        private string GetPublicIP()
        {
            // get ip using dyndns.org
            string url = "http://checkip.dyndns.org";
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            string response = sr.ReadToEnd().Trim();
            string[] a = response.Split(':');
            string a2 = a[1].Substring(1);
            string[] a3 = a2.Split('<');
            string a4 = a3[0];
            return a4;
        }
    }
}
