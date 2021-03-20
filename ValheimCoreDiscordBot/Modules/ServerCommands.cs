using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimCoreDiscordBot.Services;

namespace ValheimCoreDiscordBot.Modules
{
    public class ServerCommands : ModuleBase
    {
        private readonly IServiceProvider _service;
        private readonly ServerService _server;
        private readonly CommandService _commands;
        private readonly IConfiguration _config;

        public ServerCommands(IServiceProvider services)
        {
            _config = services.GetRequiredService<IConfiguration>();
            _server = services.GetRequiredService<ServerService>();
            _commands = services.GetRequiredService<CommandService>();
            _service = services;
        }

        [Command("Start")]
        [Summary("Starts Valheim Server")]
        public async Task StartCommand()
        {
            _server.Start();
            await ReplyAsync("Starting Server");
        }

        [Command("Stop")]
        [Summary("Stopps Valheim Server")]
        public async Task StopCommand()
        {
            _server.Stop();
            await ReplyAsync("Stopping Server");
        }

        [Command("Update")]
        [Summary("Updates Valheim Server")]
        public async Task UpdateCommand()
        {
            _server.Update();
            await ReplyAsync("Updating Server");
        }

        [Command("Status")]
        [Summary("Returns Server Details")]
        public async Task StatusCommand()
        {
            await ReplyAsync("", false, _server.GetStatus().Build());
        }

        [Command("Server")]
        [Summary("Returns IP:Port and Password for users to login")]
        public async Task ServerCommand()
        {
            await ReplyAsync("", false, _server.GetServerInfo().Build());
        }

        [Command("Help")]
        [Alias("Commands")]
        [Summary("Lists all commands available to use")]
        public async Task ListCommand()
        {
            List<CommandInfo> commands = _commands.Commands.ToList();
            EmbedBuilder embedBuilder = new EmbedBuilder();

            foreach (CommandInfo command in commands)
            {
                // Get the command Summary attribute information
                string embedFieldText = command.Summary ?? "No description available\n";

                embedBuilder.AddField(command.Name, embedFieldText);
            }
            embedBuilder.WithColor(Color.Orange);
            embedBuilder.WithFooter(footer => footer.Text = $"Commands are run using prefix '{_config["Prefix"]}' (ex. {_config["Prefix"]}help)");

            await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
        }
    }
}
