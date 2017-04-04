using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSU.Sync
{
    public class Bikey
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly List<string> KeyFolders = new List<string>();
        static Bikey()
        {
            // "standard" (ie used by the mods we use anyway) keys folders, ideally this should be loaded from somewhere else. 
            // Although most (all?) mods seem to use "keys" with the exeception of RHS. 
            KeyFolders.Add("keys");
            KeyFolders.Add("key");
        }
        /// <summary>
        /// Gets all mod folders which have bikey files inside of them in the "standard" locations. 
        /// </summary>
        /// <param name="mods"></param>
        /// <param name="localPath"></param>
        /// <returns></returns>
        internal static List<ModFolder> GetModFoldersWithBikeys(List<ModFolder> mods, string localPath)
        {
            var returnList = new List<ModFolder>();

            foreach (ModFolder m in mods)
            {
                var modPath = new DirectoryInfo(Path.Combine(localPath, m.ModName));
                DirectoryInfo[] folders = modPath.GetDirectories();
                
                // TODO: This is copied from the userconfig class, which in turn is copied from the TeamSpeak class, really needs refactoring 

                foreach (string folder in KeyFolders)
                {
                    if (!folders.Any(x => x.FullName.Equals(Path.Combine(modPath.FullName, folder)))) continue;
                    //DirectoryInfo modPluginFolder = new DirectoryInfo(Path.Combine(modPath.FullName, folder));
                    Logger.Info("Key(s) exists inside of {0}", modPath);
                    returnList.Add(m);
                }
            }

            return returnList;
        }

        public static bool CopyBiKeys(List<ModFolder> modFolders, DirectoryInfo localPath)
        {
            // Check ArmA is installed to prevent a whole host of issues happening
            if (!ArmA.IsInstalled())
            {
                return false;
            }

            List<ModFolder> keyFolders = GetModFoldersWithBikeys(modFolders, localPath.ToString());
            DirectoryInfo armAKeysFolder = Directory.CreateDirectory(Path.Combine(ArmA.ArmALocation(), "Keys"));

            foreach (ModFolder m in keyFolders)
            {
                var modPath = new DirectoryInfo(Path.Combine(localPath.ToString(), m.ModName));
                // Look in the possible folders
                foreach (string kf in KeyFolders)
                {
                    var possibleKeyFolder = new DirectoryInfo(Path.Combine(modPath.ToString(), kf));
                    if (!possibleKeyFolder.Exists) continue;
                    foreach (FileInfo f in possibleKeyFolder.GetFiles())
                    {
                        if (!File.Exists(Path.Combine(armAKeysFolder.FullName,f.Name)))
                        {
                            // Just copy it 
                            File.Copy(Path.Combine(possibleKeyFolder.ToString(), f.Name), Path.Combine(armAKeysFolder.FullName, f.Name));

                        } 
                        else 
                        {
                            // Only copy it if its different
                            if (!FileEquals(new FileInfo(Path.Combine(possibleKeyFolder.ToString(), f.Name)), new FileInfo(Path.Combine(armAKeysFolder.FullName, f.Name))))
                            {
                                File.Copy(Path.Combine(possibleKeyFolder.ToString(), f.Name), Path.Combine(armAKeysFolder.FullName, f.Name));
                            }
                                
                        }
                    }
                }
            }
            return true;


        }

        private static bool FileEquals(FileSystemInfo a, FileSystemInfo b)
        {
            // Adapted from kb320348
            int f1Byte, f2Byte;
            if (a == b)
            {
                return true;
            }
            var fs1 = new FileStream(a.FullName, FileMode.Open);
            var fs2 = new FileStream(b.FullName, FileMode.Open);

            if (fs1.Length != fs2.Length)
            {
                fs1.Close();
                fs2.Close();
                return false;
            }

            do
            {
                f1Byte = fs1.ReadByte();
                f2Byte = fs2.ReadByte();
            }
            while ((f1Byte == f2Byte) && (f1Byte != -1));

            fs1.Close();
            fs2.Close();

            return ((f1Byte - f2Byte) == 0);
        }
    }
}
