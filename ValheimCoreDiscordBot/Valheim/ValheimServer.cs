using Medallion.Shell;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimCoreDiscordBot.Services;

namespace ValheimCoreDiscordBot.Valheim
{
    public class ValheimServer
    {
        public Command cmd;
        public ValheimServerDetails Server_Details;
        public ObservableCollection<string> output;
        public List<ValheimPlayer> Players { get; set; }
        public ValheimPlayer LastPlayer { get; set; }

        private readonly IServiceProvider _services;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly IConfiguration _config;

        public ValheimServer(IServiceProvider services)
        {
            _logger = services.GetRequiredService<ILogger<CommandHandler>>();
            _config = services.GetRequiredService<IConfiguration>();
            _services = services;

            Server_Details = new ValheimServerDetails();
            output = new ObservableCollection<string>();
            ValheimServerUtilities.CheckLogFiles();
            Players = new List<ValheimPlayer>();
        }

        public async Task StartServer()
        {
            if (Server_Details.Server_Status == ServerStatus.None)
            {
                // check for any existing instance first
                await Recover();
                _logger.LogInformation("Starting Server");
                var dir = _config["ServerDir"];
                cmd = Command.Run("cmd.exe", new[] { "/c ", _config["ServerBat"] }, options => options.StartInfo(psi => psi.WorkingDirectory = dir));

                Server_Details.Server_Status = ServerStatus.Starting;
                ValheimServerUtilities.SaveProcessID(cmd.ProcessId.ToString());
                try
                {
                    string line;
                    while ((line = await cmd.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        ProcessEvent(line);
                    }

                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                _logger.LogError($"Server Status: {Server_Details.Server_Status.ToString()}");
                _logger.LogError("Server is in an unstartable Status. If the server is non responsive or not working as intended you may need to force kill the server.");
            }
        }

        private void ProcessEvent(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                if (line.Length > 19)
                {
                    var dt = line.Substring(0, 19);
                    DateTime dtout;
                    if (DateTime.TryParse(dt, out dtout))
                    {
                        ParseEvent(dtout, line.Substring(20).Trim());
                        LogEvent(dtout, line.Substring(20).Trim(), ValheimEvents.DebugEvent);
                    }
                    else
                    {
                        // debug
                        LogEvent(null, line, ValheimEvents.Debug);
                    }
                }
                else
                {
                    LogEvent(null, line, ValheimEvents.Debug);
                    // debug
                }
            }
        }

        private void ParseEvent(DateTime dtout, string message)
        {
            if (message.Contains("game server connected", StringComparison.OrdinalIgnoreCase))
            {
                LogEvent(dtout, message, ValheimEvents.GameServerConnected);
            }
            else if (message.Contains("Got connection SteamID".ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                ProcessPlayerEent(dtout, message, ValheimEvents.PlayerConnected);
            }
            else if (message.Contains("Closing socket".ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                ProcessPlayerEent(dtout, message, ValheimEvents.PlayerDisconnected);
            }
            else if (message.Contains("Got character ZDOID".ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                ProcessPlayerEent(dtout, message, ValheimEvents.PlayerCharacter);
            }
            else if (message.Contains("Found location of".ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorldEvent(dtout, message, ValheimEvents.FoundLocation);
            }
            else if (message.Contains("World saved".ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorldEvent(dtout, message, ValheimEvents.WorldSaved);
            }
            else if (message.Contains("Net scene destroyed".ToLower(), StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // clean up anything leftover
                    // the server has cleanly stopped if this message is received
                    cmd.Kill();
                    Server_Details.Server_Status = ServerStatus.None;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                LogEvent(dtout, "Server Terminated", ValheimEvents.GameServerDisconnected);
            }
            //
        }

        private void ProcessWorldEvent(DateTime dtout, string message, ValheimEvents myEvent)
        {
            LogEvent(dtout, message, myEvent);
        }

        private void ProcessPlayerEent(DateTime dtout, string message, ValheimEvents myevent)
        {
            var split = message.Split(' ');
            var steamID = split[split.Length - 1];
            switch (myevent)
            {
                case ValheimEvents.PlayerConnected:
                    var player = Players.FirstOrDefault(x => x.SteamID.Equals(steamID));
                    if (player == null)
                    {
                        player = new ValheimPlayer { SteamID = steamID };
                        Players.Add(player);
                        LogEvent(dtout, $"{steamID} has connected!", myevent);
                    }
                    LastPlayer = player;
                    break;
                case ValheimEvents.PlayerDisconnected:
                    var playerx = Players.FirstOrDefault(x => x.SteamID.Equals(steamID));
                    if (playerx != null)
                    {
                        playerx.IsLoggedIn = false;
                        LogEvent(dtout, $"{playerx.LastCharacter} has logged out!", myevent);
                    }
                    break;
                case ValheimEvents.PlayerCharacter:
                    var characterName = split[4];
                    var charsplit = steamID.Split(':');
                    var charID = charsplit[0];
                    var charSpawn = charsplit[1];
                    if (charID.Equals("0"))
                    {
                        // player died
                        var character = Players.Select(x => x.Characters.FirstOrDefault(c => c.Name.Equals(characterName))).FirstOrDefault();
                        if (character != null)
                        {
                            // Log player Death
                            LogEvent(dtout, $"{characterName} has died!", myevent);
                            character.Events.Add(new ValheimEvent { Timestamp = dtout, Message = $"{characterName} has died!", Event = ValheimEvents.PlayerCharacterDied });
                        }
                    }
                    else
                    {
                        // player spawned
                        var character = Players.Select(x => x.Characters.FirstOrDefault(c => c.ZDOID.Equals(charID))).FirstOrDefault();
                        if (character != null)
                        {
                            // log
                            if (charSpawn.Equals("1"))
                            {
                                LogEvent(dtout, $"{characterName} has Logged In!", myevent);
                                character.Events.Add(new ValheimEvent { Timestamp = dtout, Message = $"{characterName} has Logged In!", Event = ValheimEvents.PlayerCharacterSpawned });
                            }
                            else
                            {
                                LogEvent(dtout, $"{characterName} has respawned!", myevent);
                                character.Events.Add(new ValheimEvent { Timestamp = dtout, Message = $"{characterName} has respawned!", Event = ValheimEvents.PlayerCharacterSpawned });
                            }
                            // check if zdoid is same every login per character
                        }
                        else
                        {
                            character = new ValheimCharacter { ZDOID = charID, Name = characterName };
                            var pp = Players.FirstOrDefault(x => x.SteamID.Equals(LastPlayer.SteamID));
                            pp.Characters.Add(character);
                            pp.IsLoggedIn = true;
                            pp.LastCharacter = characterName;
                            // log player login??
                            if (charSpawn.Equals("1"))
                            {
                                LogEvent(dtout, $"{characterName} has Logged In!", myevent);
                                character.Events.Add(new ValheimEvent { Timestamp = dtout, Message = $"{characterName} has Logged In!", Event = ValheimEvents.PlayerCharacterSpawned });
                            }
                            else
                            {
                                LogEvent(dtout, $"{characterName} has respawned!", myevent);
                                character.Events.Add(new ValheimEvent { Timestamp = dtout, Message = $"{characterName} has respawned!", Event = ValheimEvents.PlayerCharacterSpawned });
                            }
                        }
                    }
                    break;
                default:
                    // log that a player event isn't processing
                    break;
            }
        }

        public void LogEvent(DateTime? dtout, string message, ValheimEvents myevent)
        {
            switch (myevent)
            {
                case ValheimEvents.GameServerConnected:
                case ValheimEvents.GameServerDisconnected:
                    //Console.ForegroundColor = ConsoleColor.Green;
                    //Console.WriteLine($"{dtout}| {message}");
                    output.Add(message);
                    break;
                case ValheimEvents.ClientHandshake:
                case ValheimEvents.PlayerConnected:
                case ValheimEvents.PlayerDisconnected:
                case ValheimEvents.PlayerCharacter:
                case ValheimEvents.PlayerCharacterDied:
                    //Console.ForegroundColor = ConsoleColor.Yellow;
                    //Console.WriteLine($"{dtout}| {message}");
                    output.Add(message);
                    break;
                case ValheimEvents.WorldSaved:
                case ValheimEvents.FoundLocation:
                    //Console.ForegroundColor = ConsoleColor.Magenta;
                    //Console.WriteLine($"{dtout}| {message}");
                    output.Add(message);
                    break;
                case ValheimEvents.Debug:
                    //Console.ForegroundColor = ConsoleColor.Gray;
                    //Console.WriteLine($"{message}");
                    _logger.LogInformation($"{dtout}| {message}");
                    break;
                case ValheimEvents.DebugEvent:
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine($"{dtout}| {message}");
                    _logger.LogInformation($"{dtout}| {message}");
                    break;
                case ValheimEvents.VersionCheck:
                    output.Add(message);
                    break;
                default:
                    //Console.ForegroundColor = ConsoleColor.Gray;
                    //Console.WriteLine($"{dtout}| {message}");
                    _logger.LogInformation($"{dtout}| {message}");
                    break;
            }
        }

        internal async Task Recover()
        {
            var id = ValheimServerUtilities.GetProcessID();
            int pID;
            var valid = int.TryParse(id, out pID);
            if (valid && Command.TryAttachToProcess(pID, out cmd))
            {
                Server_Details.Server_Status = ServerStatus.Recovered;
                _logger.LogInformation("Recoverd connection");
                await KillServer();
                _logger.LogInformation("Terminating Server");
            }
            else
            {
                _logger.LogInformation("Nothing to recover!");
            }
        }

        public async Task KillServer()
        {
            await cmd.TrySignalAsync(CommandSignal.ControlC);
            Server_Details.Server_Status = ServerStatus.Terminating;
            //cmd.Kill();
            
            ValheimServerUtilities.ClearProcessID();
        }
    }
}
