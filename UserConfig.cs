using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace BSU.Sync
{
    public class UserConfig
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Finds all mod folders which have a userconfig in it. 
        /// </summary>
        /// <param name="mods"></param>
        /// <param name="localPath"></param>
        /// <returns></returns>
        internal static List<ModFolder> GetModFoldersWithUserConfigs(List<ModFolder> mods, string localPath)
        {
            var returnList = new List<ModFolder>();

            foreach (ModFolder m in mods)
            {
                var modPath = new DirectoryInfo(Path.Combine(localPath, m.ModName));
                DirectoryInfo[] folders = modPath.GetDirectories();
                var pluginFolders = new List<string> {"userconfig"};
                // Doing it this way just in case in the future some addons have userconfigs in non standard folders
                // TODO: This is copied from TeamSpeakPlugin so it should at some point be moved to its own class.
                foreach (string folder in pluginFolders)
                {
                    if (!folders.Any(x => x.FullName.Equals(Path.Combine(modPath.FullName, folder)))) continue;
                    //var modPluginFolder = new DirectoryInfo(Path.Combine(modPath.FullName, folder));
                    Logger.Trace("Userconfig(s) exists inside of {0}", modPath);
                    returnList.Add(m);
                }
            }

            return returnList;
        }

        public static bool CopyUserConfigs(List<ModFolder> modFolders, DirectoryInfo localPath)
        {

            // Final check
            if (!ArmA.IsInstalled())
            {
                return false;
            }

            List<ModFolder> userConfigFolders = GetModFoldersWithUserConfigs(modFolders, localPath.ToString());
            var armAUserConfigFolder = new DirectoryInfo(Path.Combine(ArmA.ArmALocation(), "userconfig"));

            foreach (ModFolder m in userConfigFolders)
            {
                // Find the actual folders inside of the userconfig 
                var modPath = new DirectoryInfo(Path.Combine(localPath.ToString(), m.ModName));
                var userConfigFolder = new DirectoryInfo(Path.Combine(modPath.ToString(), "userconfig"));
                foreach (DirectoryInfo d in userConfigFolder.GetDirectories())
                {
                    var userconfigUpdateSide = new DirectoryInfo(Path.Combine(userConfigFolder.ToString(), d.Name));
                    DirectoryInfo userconfigArmASide = Directory.CreateDirectory(Path.Combine(armAUserConfigFolder.ToString(), d.Name));
                    // Copy every file from inside of the userconfig folder, only if it does not exist in the destination. 
                    // TODO: Find a way to flag a userconfig as needing to be replaced, as the name suggests they may contain user customisations so they cannot just be overwritten every time like some files
                    foreach (FileInfo f in userconfigUpdateSide.GetFiles())
                    {
                        if (!File.Exists(Path.Combine(userconfigArmASide.FullName, f.Name)))
                        {
                            Logger.Trace("{0} does not exist, copying", f);
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
