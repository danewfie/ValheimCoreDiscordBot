using System;
using System.Collections.Generic;
using System.Text;

namespace ValheimCoreDiscordBot.Valheim
{
    public class ValheimPlayer
    {
        public string SteamID { get; set; }
        public bool IsLoggedIn { get; set; }
        public string LastCharacter { get; set; }
        public List<ValheimCharacter> Characters { get; set; }
        public Player SteamPlayer { get; set; }

        public ValheimPlayer()
        {
            Characters = new List<ValheimCharacter>();
        }
    }

    public class ValheimCharacter
    {
        public string Name { get; set; }
        public string ZDOID { get; set; }
        public List<ValheimEvent> Events { get; set; }

        public ValheimCharacter()
        {
            Events = new List<ValheimEvent>();
        }
    }

    public class ValheimEvent
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public ValheimEvents Event { get; set; }
    }

    public enum ValheimEvents
    {
        GameServerConnected,
        GameServerDisconnected,
        PlayerConnected,
        PlayerDisconnected,
        PlayerCharacter,
        PlayerCharacterDied,
        PlayerCharacterSpawned,
        VersionCheck,
        ClientHandshake,
        WorldSaved,
        FoundLocation,
        Debug,
        DebugEvent,
        GameStatusUpdate
    }
}
