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

        public string GetGameState()
        { // generates message to use for Discord Playing message
            string gamestate = string.Empty;
            switch (Server_Details.Server_Status)
            {
                case ServerStatus.Starting:
                    gamestate = "Valheim Starting";
                    break;
                case ServerStatus.Terminating:
                    gamestate = "Valheim Stopping";
                    break;
                case ServerStatus.Running:                    
                    gamestate = $"{GetPlayerCount()} Online";
                    break;
                case ServerStatus.Recovered:
                case ServerStatus.Offline:
                default:
                    break;
            }
            return gamestate;
        }

        private string GetPlayerCount()
        { // counts how many players are online
            var count = Players.Count(x => x.IsLoggedIn);

            return $"{count} Players";
        }

        public async Task StartServer()
        {
            if (Server_Details.Server_Status == ServerStatus.Offline)
            {
                Server_Details.Server_Status = ServerStatus.Starting;
                // check for any existing instance first
                await Recover();
                _logger.LogInformation("Starting Server");
                var dir = _config["ServerDir"];
                cmd = Command.Run("cmd.exe", new[] { "/c ", _config["ServerBat"] }, options => options.StartInfo(psi => psi.WorkingDirectory = dir));

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
                    _logger.LogError(ex.ToString());
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
        { // logic to parse events coming from the server output
            if (message.Contains("game server connected", StringComparison.OrdinalIgnoreCase))
            {
                LogEvent(dtout, message, ValheimEvents.GameServerConnected);
                Server_Details.Server_Status = ServerStatus.Running;
                Server_Details.Server_StartTime = DateTime.Now;
                LogEvent(null, "~| Game Status Update", ValheimEvents.GameStatusUpdate);
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
                    Server_Details.Server_Status = ServerStatus.Offline;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                LogEvent(dtout, "Server Terminated", ValheimEvents.GameServerDisconnected);
                LogEvent(null, "~| Game Status Update", ValheimEvents.GameStatusUpdate);
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
                        player = new ValheimPlayer { SteamID = steamID, IsLoggedIn = true };
                        Players.Add(player);
                    }
                    player.IsLoggedIn = true;
                    try
                    {
                        if (_config["SteamAPIKey"] != null)
                        {
                            player.SteamPlayer = ValheimServerUtilities.GetSteamInfo(steamID).Result.response.players.FirstOrDefault();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                    // setting debug event so it doesn't message discord
                    LogEvent(dtout, $"{steamID} has connected!", ValheimEvents.DebugEvent);
                    LogEvent(null, "~| Game Status Update", ValheimEvents.GameStatusUpdate);
                    LastPlayer = player;
                    break;
                case ValheimEvents.PlayerDisconnected:
                    var playerx = Players.FirstOrDefault(x => x.SteamID.Equals(steamID));
                    if (playerx != null)
                    {
                        playerx.IsLoggedIn = false;
                        LogEvent(dtout, $"{playerx.LastCharacter} has logged out!", myevent);
                        LogEvent(null, "~| Game Status Update", ValheimEvents.GameStatusUpdate);
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
                    output.Add(message);
                    break;
                case ValheimEvents.ClientHandshake:
                    // don't send messages here for now
                    _logger.LogInformation($"{dtout}| {message}");
                    break;
                case ValheimEvents.PlayerConnected:
                case ValheimEvents.PlayerDisconnected:
                case ValheimEvents.PlayerCharacter:
                case ValheimEvents.PlayerCharacterDied:
                    output.Add(message);
                    break;
                case ValheimEvents.WorldSaved:
                    // do not send message for world save
                    if (Server_Details.Server_Status.Equals(ServerStatus.Terminating))
                    {
                        output.Add(message);
                    }
                    _logger.LogInformation($"{dtout}| {message}");
                    break;
                case ValheimEvents.FoundLocation:
                    output.Add(message);
                    break;
                case ValheimEvents.Debug:
                    _logger.LogInformation($"{dtout}| {message}");
                    break;
                case ValheimEvents.DebugEvent:
                    _logger.LogInformation($"{dtout}| {message}");
                    break;
                case ValheimEvents.VersionCheck:
                    output.Add(message);
                    break;
                case ValheimEvents.GameStatusUpdate:
                    output.Add(message);
                    break;
                default:
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
            Server_Details.Server_Status = ServerStatus.Terminating;
            await cmd.TrySignalAsync(CommandSignal.ControlC);
            //cmd.Kill();
            
            ValheimServerUtilities.ClearProcessID();
        }
    }
}
