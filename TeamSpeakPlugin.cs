using Microsoft.Win32;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSU.Sync
{
    public static class TeamSpeakPlugin
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
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
        /// <summary>
        /// Gets a list of all mod folders which contain 
        /// </summary>
        /// <param name="Mods"></param>
        /// <param name="LocalPath"></param>
        /// <returns></returns>
        public static List<ModFolder> GetModFoldersWithPlugins(List<ModFolder> Mods, string LocalPath)
        {
            List<ModFolder> returnList = new List<ModFolder>();

            foreach (ModFolder m in Mods)
            {
                DirectoryInfo modPath = new DirectoryInfo(Path.Combine(LocalPath.ToString(), m.ModName.ToString()));
                DirectoryInfo[] folders = modPath.GetDirectories();
                List<string> pluginFolders = new List<string>();
                pluginFolders.Add("plugins");
                pluginFolders.Add("plugin");
                // ^ Support for both TFAR and ACRE 
                foreach (string folder in pluginFolders)
                {
                    if (folders.Any(x => x.FullName.Equals(Path.Combine(modPath.FullName, folder))))
                    {
                        DirectoryInfo modPluginFolder = new DirectoryInfo(Path.Combine(modPath.FullName, folder));
                        DirectoryInfo tsPluginFolder = new DirectoryInfo(Path.Combine(TeamSpeakPlugin.TeamSpeakPath(), folder));

                        logger.Trace("Plugins exists inside of {0}", modPath);
                        returnList.Add(m);
                    }
                }
            }

            return returnList;
        }
    }
}
