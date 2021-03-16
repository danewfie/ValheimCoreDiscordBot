using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ValheimCoreDiscordBot.Valheim
{
    public class ValheimServerUtilities
    {
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
    }

    public enum ServerStatus
    {
        Starting,
        Terminating,
        Running,
        Recovered,
        None
    }
}
