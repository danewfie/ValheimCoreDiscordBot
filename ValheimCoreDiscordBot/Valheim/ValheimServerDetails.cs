using System;
using System.Collections.Generic;
using System.Text;

namespace ValheimCoreDiscordBot.Valheim
{
    public class ValheimServerDetails
    {
        public ServerStatus Server_Status = ServerStatus.Offline;
        public string World_Seed { get; set; }
        public DateTime Server_StartTime { get; set; }
        public string Server_ID { get; set; }
        public string Connections { get; set; }

        public TimeSpan Server_Runtime { get {
                return Server_Status.Equals(ServerStatus.Running) ? DateTime.Now.Subtract(Server_StartTime): new TimeSpan();
            } }
    }
}
