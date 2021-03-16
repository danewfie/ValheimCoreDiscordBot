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

        private void ProcessOutput(string message)
        {
            var channel = _client.GetChannel(ChannelToken) as ISocketMessageChannel;
            channel.SendMessageAsync(message);
        }

        public string Start()
        {
            _client.SetGameAsync("Valheim Server");
            _server.StartServer();
            //var channel = _client.GetChannel(704348887263084608) as ISocketMessageChannel;
            //channel.SendMessageAsync("sent from service");
            return "Starting";
        }

        public string Stop()
        {
            _client.SetGameAsync("");
            _server.KillServer();
            return "Terminating";
        }

        public string GetStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Status: {MyStatus}");
            sb.AppendLine($"Started: {dateTime}");

            return sb.ToString();
        }

        public string GetServerInfo()
        {
            // initialize empty string builder for reply
            var sb = new StringBuilder();
            // append server IP and port
            sb.AppendLine($"{GetPublicIP()}:{_config["Port"]}");
            // append server password
            sb.AppendLine($"{_config["Password"]}");

            return sb.ToString();
        }

        private string GetPublicIP()
        {
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
