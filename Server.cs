using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSO.Sync.FileTypes;
using System.IO;

namespace BSO.Sync
{
    internal struct ModFolderHash
    {
        internal ModFolder ModName;
        internal List<HashType> Hashes;
        internal ModFolderHash(ModFolder ModName, List<HashType> Hashes)
        {
            this.ModName = ModName;
            this.Hashes = Hashes;
        }
    }
    public class Server
    {
        string LocalPath;
        string ServerName;
        string ServerAddress;
        string Password;
        List<ModFolder> Mods;
        DateTime CreationDate;
        DateTime LastUpdate;
        List<ModFolderHash> ModHashes;
        Guid ServerGuid;
        

        public void CreateNewServer(string ServerName, string ServerAddress, string Password, string LPath, string OutputPath)
        {
            this.ServerAddress = ServerAddress;
            this.ServerName = ServerName;
            this.Password = Password;
            CreationDate = DateTime.Now;
            LastUpdate = DateTime.Now;
            ServerGuid = Guid.NewGuid();
            Mods = GetFolders(new DirectoryInfo(LPath));
            // As we are creating a new server the LocalPath is actually the OutputPath, so we don't taint the local copy of the mods
            LocalPath = OutputPath;
            FileWriter.WriteServerConfig(GetServerFile(), new FileInfo(Path.Combine(LPath, "server.json")));
            FileCopy.CopyAll(new DirectoryInfo(LPath), new DirectoryInfo(OutputPath));
            FileCopy.CleanUpFolder(new DirectoryInfo(LPath), new DirectoryInfo(LocalPath), new DirectoryInfo(LocalPath));
            ModHashes = HashAllMods();
            FileWriter.WriteModHashes(ModHashes, new DirectoryInfo(LocalPath));
        }
        List<ModFolder> GetFolders()
        {
            return GetFolders(new DirectoryInfo(LocalPath));
        }
        List<ModFolder> GetFolders(DirectoryInfo FilePath)
        {
            List<ModFolder> returnList = new List<ModFolder>();
            foreach (string d in Directory.GetDirectories(FilePath.FullName))
            {
                returnList.Add(new ModFolder(d.Replace(FilePath.FullName, string.Empty).Replace(@"\", string.Empty)));
            }
            return returnList;
        }
        List<ModFolderHash> HashAllMods()
        {
            List<ModFolderHash> Hashes = new List<ModFolderHash>();
            foreach (ModFolder mod in Mods)
            {
                Console.WriteLine("hashing {0}", mod.ModName);
                List<HashType> hashes = Hash.HashFolder(LocalPath + @"\" + mod.ModName);
                Hashes.Add(new ModFolderHash(mod, hashes));
            }
            return Hashes;
        }
        public FileTypes.ServerFile GetServerFile()
        {
            return new FileTypes.ServerFile(ServerName, ServerAddress, Password, Mods,LastUpdate,CreationDate,ServerGuid);
        }
        public void LoadServer(FileTypes.ServerFile sf, string LocalPath)
        {
            this.LocalPath = LocalPath;
            ServerName = sf.ServerName;
            ServerAddress = sf.ServerAddress;
            Password = sf.Password;
            Mods = sf.ModFolders;
            LastUpdate = sf.LastUpdateDate;
            CreationDate = sf.CreationDate;
            ServerGuid = sf.ServerGUID;
        }
    }
}
