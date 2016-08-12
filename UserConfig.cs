using BSU.Sync;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSO.Sync
{
    public class UserConfig
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Finds all mod folders which have a userconfig in it. 
        /// </summary>
        /// <param name="Mods"></param>
        /// <param name="LocalPath"></param>
        /// <returns></returns>
        internal static List<ModFolder> GetModFoldersWithUserConfigs(List<ModFolder> Mods, string LocalPath)
        {
            List<ModFolder> returnList = new List<ModFolder>();

            foreach (ModFolder m in Mods)
            {
                DirectoryInfo modPath = new DirectoryInfo(Path.Combine(LocalPath.ToString(), m.ModName.ToString()));
                DirectoryInfo[] folders = modPath.GetDirectories();
                List<string> pluginFolders = new List<string>();
                // Doing it this way just in case in the future some addons have userconfigs in non standard folders
                // TODO: This is copied from TeamSpeakPlugin so it should at some point be moved to its own class.
                pluginFolders.Add("userconfig");
                foreach (string folder in pluginFolders)
                {

                    if (folders.Any(x => x.FullName.Equals(Path.Combine(modPath.FullName, folder))))
                    {
                        DirectoryInfo modPluginFolder = new DirectoryInfo(Path.Combine(modPath.FullName, folder));
                        logger.Trace("Userconfig(s) exists inside of {0}", modPath);
                        returnList.Add(m);
                    }
                }
            }

            return returnList;
        }

        public static bool CopyUserConfigs(List<ModFolder> ModFolders, DirectoryInfo LocalPath)
        {

            // Final check
            if (!ArmA.IsInstalled())
            {
                return false;
            }

            var UserConfigFolders = GetModFoldersWithUserConfigs(ModFolders, LocalPath.ToString());
            DirectoryInfo ArmAUserConfigFolder = new DirectoryInfo(Path.Combine(ArmA.ArmALocation(), "userconfig"));

            foreach (ModFolder m in UserConfigFolders)
            {
                // Find the actual folders inside of the userconfig 
                DirectoryInfo modPath = new DirectoryInfo(Path.Combine(LocalPath.ToString(), m.ModName.ToString()));
                DirectoryInfo userConfigFolder = new DirectoryInfo(Path.Combine(modPath.ToString(), "userconfig"));
                foreach (DirectoryInfo d in userConfigFolder.GetDirectories())
                {
                    DirectoryInfo userconfigUpdateSide = new DirectoryInfo(Path.Combine(userConfigFolder.ToString(), d.Name));
                    DirectoryInfo userconfigArmASide = Directory.CreateDirectory(Path.Combine(ArmAUserConfigFolder.ToString(), d.Name));
                    // Copy every file from inside of the userconfig folder, only if it does not exist in the destination. 
                    // TODO: Find a way to flag a userconfig as needing to be replaced, as the name suggests they may contain user customisations so they cannot just be overwritten every time like some files
                    foreach (FileInfo f in userconfigUpdateSide.GetFiles())
                    {
                        if (!File.Exists(Path.Combine(userconfigArmASide.FullName, f.Name)))
                        {
                            logger.Trace("{0} does not exist, copying", f);
                            // Copy the file..
                            File.Copy(f.FullName, Path.Combine(userconfigArmASide.FullName, f.Name));
                        }
                    }
                }
            }

            return true;
        }
    }
}
