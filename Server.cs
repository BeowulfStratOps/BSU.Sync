using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSO.Sync.FileTypes;
using System.IO;
using System.Net;
using System.Threading;

namespace BSO.Sync
{
    public struct ModFolderHash
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
        List<Uri> SyncUris;
        
        public void LoadFromWeb(Uri RemoteServerFile, DirectoryInfo LocalPath)
        {
            this.LocalPath = LocalPath.ToString();
            WebRequest request = WebRequest.CreateHttp(RemoteServerFile);
            LoadServer(FileReader.ReadServerFileFromStream(request.GetResponse().GetResponseStream()), LocalPath.ToString());
            ModHashes = HashAllMods();
        }
        public void CreateNewServer(string ServerName, string ServerAddress, string Password, string LPath, string OutputPath, List<Uri> SyncUris)
        {
            this.ServerAddress = ServerAddress;
            this.ServerName = ServerName;
            this.Password = Password;
            this.SyncUris = SyncUris;
            CreationDate = DateTime.Now;
            LastUpdate = DateTime.Now;
            ServerGuid = Guid.NewGuid();
            LocalPath = OutputPath;
            this.SyncUris = SyncUris;
            UpdateServer(new DirectoryInfo(LPath));

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
            return new FileTypes.ServerFile(ServerName, ServerAddress, Password, Mods,LastUpdate,CreationDate,ServerGuid,SyncUris);
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
            SyncUris = sf.SyncUris;
        }
        public void UpdateServer(DirectoryInfo InputDirectory)
        {
            LastUpdate = DateTime.Now;
            Mods = GetFolders(InputDirectory);
            FileWriter.WriteServerConfig(GetServerFile(), new FileInfo(Path.Combine(InputDirectory.FullName, "server.json")));
            FileCopy.CopyAll(InputDirectory, new DirectoryInfo(LocalPath));
            FileCopy.CleanUpFolder(InputDirectory, new DirectoryInfo(LocalPath), new DirectoryInfo(LocalPath));
            // TODO: Maybe remove all zsync files?
            ModHashes = HashAllMods();
            foreach (string f in Directory.EnumerateFiles(LocalPath,"*",SearchOption.AllDirectories).Where(name => !name.EndsWith(".zsync")))
            {
                ZsyncManager.Make(f);
            }
            FileWriter.WriteModHashes(ModHashes, new DirectoryInfo(LocalPath));

        }
        public void FetchChanges(DirectoryInfo BaseDirectory, List<ModFolderHash> NewHashes)
        {
            List<Change> Changes = GenerateChangeList(NewHashes);
            List<Task> tasks = new List<Task>();
            foreach (Change c in Changes)
            {
                if (c.Action == ChangeAction.Acquire)
                {
                    //Changes.Remove(c);
                    if (c.FilePath != "server.json")
                    {
                        //Console.WriteLine("Getting {0}",c.FilePath);
                        Uri reqUri = new Uri(SyncUris[0], c.FilePath + ".zsync");
                        try
                        {
                            //ZsyncManager.ZsyncDownload(reqUri, BaseDirectory.ToString(), c.FilePath);

                            Task t = Task.Factory.StartNew(() => ZsyncManager.ZsyncDownload(reqUri, BaseDirectory.ToString(), c.FilePath));
                            tasks.Add(t);
                            //Console.WriteLine("Created task for {0}", c.FilePath);

                        }
                        catch (Exception ex)
                        {
                            if (ex is com.salesforce.zsync.ZsyncChecksumValidationFailedException)
                            {
                                Console.WriteLine("Checksum Validation failed for {0}", c.FilePath);
                            }
                            // TODO: Add to a reacquire list and log the error
                        }
                    }

                }
                else if (c.Action == ChangeAction.Delete)
                {
                    Console.WriteLine(Path.Combine(BaseDirectory.ToString(), c.FilePath));
                    //Changes.Remove(c);
                }
            }
            Task.WaitAll(tasks.ToArray());
            
            
        }
        public List<Change> GenerateChangeList(List<ModFolderHash> NewHashes)
        {
            List<Change> ChangeList = new List<Change>();
            foreach (ModFolderHash mfh in NewHashes)
            {
                if (!ModHashes.Exists(x => x.ModName.ModName == mfh.ModName.ModName))
                {
                    // If the entire mod doesn't exist, add it all
                    foreach (HashType h in mfh.Hashes)
                    {
                        ChangeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire));
                    }
                }
                else
                {
                    int indexInLocalHash = ModHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                    int indexInNewHash = NewHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                    foreach (HashType h in mfh.Hashes)
                    {
                        if (ModHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName))
                        {
                            // File exists both in the local hash and the remote hash
                            if (ModHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName && !x.Hash.SequenceEqual(h.Hash)))
                            {
                                // A file exists but has a different hash, it must be (re)acquired 
                                HashType hash = ModHashes[indexInLocalHash].Hashes.Find(x => x.FileName == h.FileName);
                                ChangeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire));
                            }
                        }
                        else if (!ModHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName) && NewHashes[indexInNewHash].Hashes.Exists(x => x.FileName == h.FileName ))
                        {
                            // Does not exist locally, but does exist remotely. Acquire it
                            ChangeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire));
                        }
                        else if (ModHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName) && !NewHashes[indexInNewHash].Hashes.Exists(x => x.FileName == h.FileName))
                        {
                            // Exists locally, but does not exist remotely. Delete it
                            ChangeList.Add(new Change(mfh.ModName.ModName +  h.FileName, ChangeAction.Delete));
                        }
                    }
                }
            }
            return ChangeList;
        }
        public List<ModFolderHash> GetLocalHashes()
        {
            return ModHashes;
        }
    }
}
