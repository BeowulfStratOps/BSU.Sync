﻿using Microsoft.Win32;
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
        /// <param name="mods"></param>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public static List<ModFolder> GetModFoldersWithPlugins(List<ModFolder> mods, string localPath)
        {
            var returnList = new List<ModFolder>();

            foreach (ModFolder m in mods)
            {
                var modPath = new DirectoryInfo(Path.Combine(localPath, m.ModName));
                DirectoryInfo[] folders = modPath.GetDirectories();
                var pluginFolders = new List<string> {"plugins", "plugin"};
                // ^ Support for both TFAR and ACRE 
                foreach (string folder in pluginFolders)
                {
                    if (!folders.Any(x => x.FullName.Equals(Path.Combine(modPath.FullName, folder)))) continue;
                    //var modPluginFolder = new DirectoryInfo(Path.Combine(modPath.FullName, folder));
                    //var tsPluginFolder = new DirectoryInfo(Path.Combine(TeamSpeakPlugin.TeamSpeakPath(), folder));

                    logger.Trace("Plugins exists inside of {0}", modPath);
                    returnList.Add(m);
                }
            }

            return returnList;
        }
    }
}
