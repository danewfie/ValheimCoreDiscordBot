using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ValheimCoreDiscordBot.Services;

namespace ValheimCoreDiscordBot.Modules
{
    public class ServerCommands : ModuleBase
    {
        private readonly IServiceProvider _service;
        private readonly ServerService _server;

        public ServerCommands(IServiceProvider services)
        {
            _server = services.GetRequiredService<ServerService>();
            _service = services;
        }

        [Command("Start")]
        public async Task StartCommand()
        {
            _server.Start();
            await ReplyAsync("Starting Server");
        }

        [Command("Stop")]
        public async Task StopCommand()
        {
            _server.Stop();
            await ReplyAsync("Stopping Server");
        }

        [Command("Status")]
        public async Task StatusCommand()
        {
            await ReplyAsync(_server.GetStatus());
        }

        [Command("server")]
        public async Task ServerCommand()
        {
            await ReplyAsync(_server.GetServerInfo());
        }

        [Command("commands")]
        public async Task ListCommand()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("start");
            sb.AppendLine("stop");
            sb.AppendLine("status");
            sb.AppendLine("server");
            //sb.AppendLine("upodate");
            await ReplyAsync(sb.ToString());
        }
    }
}
