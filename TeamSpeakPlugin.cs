using Microsoft.Win32;

namespace BSU.Sync
{
    public static class TeamSpeakPlugin
    {
        /// <summary>
        /// Determines if TeamSpeak is installed 
        /// </summary>
        /// <returns></returns>
        public static bool TeamSpeakInstalled()
        {
            return TeamSpeakPath() != string.Empty;
        }
        /// <summary>
        /// Determines the location of TeamSpeak
        /// </summary>
        /// <returns></returns>
        public static string TeamSpeakPath()
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(@"SOFTWARE\TeamSpeak 3 Client");
            if (localKey != null)
            {
                return localKey.GetValue(null).ToString();
            }

            RegistryKey localkey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            localkey32 = localkey32.OpenSubKey(@"SOFTWARE\TeamSpeak 3 Client");
            if (localkey32 != null)
            {
                return localkey32.GetValue(null).ToString();
            }

            return string.Empty;
        }
    }
}
