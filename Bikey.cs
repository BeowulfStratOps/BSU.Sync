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
    public class Bikey
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static List<string> keyFolders = new List<string>();
        static Bikey()
        {
            // "standard" (ie used by the mods we use anyway) keys folders, ideally this should be loaded from somewhere else. 
            // Although most (all?) mods seem to use "keys" with the exeception of RHS. 
            keyFolders.Add("keys");
            keyFolders.Add("key");
        }
        /// <summary>
        /// Gets all mod folders which have bikey files inside of them in the "standard" locations. 
        /// </summary>
        /// <param name="Mods"></param>
        /// <param name="LocalPath"></param>
        /// <returns></returns>
        internal static List<ModFolder> GetModFoldersWithBikeys(List<ModFolder> Mods, string LocalPath)
        {
            List<ModFolder> returnList = new List<ModFolder>();

            foreach (ModFolder m in Mods)
            {
                DirectoryInfo modPath = new DirectoryInfo(Path.Combine(LocalPath.ToString(), m.ModName.ToString()));
                DirectoryInfo[] folders = modPath.GetDirectories();
                
                // TODO: This is copied from the userconfig class, which in turn is copied from the TeamSpeak class, really needs refactoring 

                foreach (string folder in keyFolders)
                {
                    if (folders.Any(x => x.FullName.Equals(Path.Combine(modPath.FullName, folder))))
                    {
                        DirectoryInfo modPluginFolder = new DirectoryInfo(Path.Combine(modPath.FullName, folder));
                        logger.Info("Key(s) exists inside of {0}", modPath);
                        returnList.Add(m);
                    }
                }
            }

            return returnList;
        }

        public static bool CopyBiKeys(List<ModFolder> ModFolders, DirectoryInfo LocalPath)
        {
            // Check ArmA is installed to prevent a whole host of issues happening
            if (!ArmA.IsInstalled())
            {
                return false;
            }

            var KeyFolders = GetModFoldersWithBikeys(ModFolders, LocalPath.ToString());
            DirectoryInfo ArmAKeysFolder = Directory.CreateDirectory(Path.Combine(ArmA.ArmALocation(), "Keys"));

            foreach (ModFolder m in KeyFolders)
            {
                DirectoryInfo modPath = new DirectoryInfo(Path.Combine(LocalPath.ToString(), m.ModName.ToString()));
                // Look in the possible folders
                foreach (string kf in keyFolders)
                {
                    DirectoryInfo possibleKeyFolder = new DirectoryInfo(Path.Combine(modPath.ToString(), kf));
                    if (possibleKeyFolder.Exists)
                    {
                        foreach (FileInfo f in possibleKeyFolder.GetFiles())
                        {
                            if (!File.Exists(Path.Combine(ArmAKeysFolder.FullName,f.Name)))
                            {
                                // Just copy it 
                                File.Copy(Path.Combine(possibleKeyFolder.ToString(), f.Name), Path.Combine(ArmAKeysFolder.FullName, f.Name));

                            } 
                            else 
                            {
                                // Only copy it if its different
                                if (!FileEquals(new FileInfo(Path.Combine(possibleKeyFolder.ToString(), f.Name)), new FileInfo(Path.Combine(ArmAKeysFolder.FullName, f.Name))))
                                {
                                    File.Copy(Path.Combine(possibleKeyFolder.ToString(), f.Name), Path.Combine(ArmAKeysFolder.FullName, f.Name));
                                }
                                
                            }
                        }
                    }
                }
            }
            return true;


        }

        private static bool FileEquals(FileInfo A, FileInfo B)
        {
            // Adapted from kb320348
            FileStream fs1, fs2;
            int f1byte, f2byte;
            if (A == B)
            {
                return true;
            }
            fs1 = new FileStream(A.FullName, FileMode.Open);
            fs2 = new FileStream(B.FullName, FileMode.Open);

            if (fs1.Length != fs2.Length)
            {
                fs1.Close();
                fs2.Close();
                return false;
            }

            do
            {
                f1byte = fs1.ReadByte();
                f2byte = fs2.ReadByte();
            }
            while ((f1byte == f2byte) && (f1byte != -1));

            fs1.Close();
            fs2.Close();

            return ((f1byte - f2byte) == 0);
        }
    }
}
