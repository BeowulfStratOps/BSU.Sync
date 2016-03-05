using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSU.Sync.FileTypes;
using System.IO;
using System.Net;
using System.Threading;
using NLog;

namespace BSU.Sync
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
        private Logger logger = LogManager.GetCurrentClassLogger();
        string LocalPath;
        string ServerName;
        string ServerAddress;
        int ServerPort;
        string Password;
        List<ModFolder> Mods;
        DateTime CreationDate;
        DateTime LastUpdate;
        List<ModFolderHash> ModHashes;
        Guid ServerGuid;
        List<Uri> SyncUris;
        
        public void LoadFromWeb(Uri RemoteServerFile, DirectoryInfo LocalPath)
        {
            logger.Info("Loading server from {0}, local path {1}", RemoteServerFile, LocalPath);
            this.LocalPath = LocalPath.ToString();
            WebRequest request = WebRequest.CreateHttp(RemoteServerFile);
            LoadServer(FileReader.ReadServerFileFromStream(request.GetResponse().GetResponseStream()), LocalPath.ToString());
            ModHashes = HashAllMods();
        }
        public void CreateNewServer(string ServerName, string ServerAddress, string Password, int ServerPort, string LPath, string OutputPath, List<Uri> SyncUris)
        {
            logger.Info("Creating new server: ServerName {0}, ServerAddress {1}, Password {2}, ServerPort {3}, LPath {4}, OutputPath {5}, SyncUri[0] {6}", ServerName, ServerAddress, Password, ServerPort, LPath, OutputPath, SyncUris[0]);
            this.ServerAddress = ServerAddress;
            this.ServerName = ServerName;
            this.ServerPort = ServerPort;
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
            logger.Info("Finding folders");
            List<ModFolder> returnList = new List<ModFolder>();
            foreach (string d in Directory.GetDirectories(FilePath.FullName))
            {
                logger.Info("Found folder {0}", d.Replace(FilePath.FullName, string.Empty).Replace(@"\", string.Empty));
                returnList.Add(new ModFolder(d.Replace(FilePath.FullName, string.Empty).Replace(@"\", string.Empty)));
            }
            return returnList;
        }
        List<ModFolderHash> HashAllMods()
        {
            logger.Info("Hashing all mods");
            List<Task> taskList = new List<Task>();
            List<ModFolderHash> Hashes = new List<ModFolderHash>(Mods.Count);
            foreach (ModFolder mod in Mods)
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    logger.Info("Hashing {0}", mod.ModName);
                    List<HashType> hashes = Hash.HashFolder(LocalPath + @"\" + mod.ModName);
                    Hashes.Add(new ModFolderHash(mod, hashes));
                });
                t.ContinueWith((pTask) =>
                {
                    logger.Info("Hashed {0}", mod.ModName);
                });
                taskList.Add(t);

            }
            Task.WaitAll(taskList.ToArray());
            return Hashes;
        }
        public FileTypes.ServerFile GetServerFile()
        {
            return new FileTypes.ServerFile(ServerName, ServerAddress, ServerPort, Password, Mods,LastUpdate,CreationDate,ServerGuid,SyncUris);
        }
        public void LoadServer(FileTypes.ServerFile sf, string LocalPath)
        {
            this.LocalPath = LocalPath;
            ServerName = sf.ServerName;
            ServerAddress = sf.ServerAddress;
            ServerPort = sf.ServerPort;
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
        /// <summary>
        /// Returns a list of all the mods this server is aware of
        /// </summary>
        /// <returns></returns>
        public List<ModFolder> GetLoadedMods()
        {
            return Mods;
        }
        public void FetchChanges(DirectoryInfo BaseDirectory, List<ModFolderHash> NewHashes)
        {
            List<Change> Changes = GenerateChangeList(NewHashes);
            List<Task> tasks = new List<Task>();
            foreach (Change c in Changes)
            {
                Console.WriteLine(c.Action);
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
                            /*
                            if (tasks.Count > 5)
                            { 
                                Task.WaitAll(tasks.ToArray());
                            }
                            */
                            while (tasks.Count > 5)
                            {

                            }
                            Task t = Task.Factory.StartNew(() => {
                                //Console.WriteLine("Starting");
                                ZsyncManager.ZsyncDownload(reqUri, BaseDirectory.ToString(), c.FilePath);
                            });
                            t.ContinueWith((prevTask) => {
                                //Console.WriteLine("Ending");
                                tasks.Remove(t);
                            } );
                            tasks.Add(t);
                            //Console.WriteLine("Created task for {0}", c.FilePath);

                        }
                        catch (Exception ex)
                        {
                            if (ex is com.salesforce.zsync.ZsyncChecksumValidationFailedException)
                            {
                                logger.Error(ex, "Checksum Validation failed for {0}", c.FilePath);
                            }
                            // TODO: Add to a reacquire list and log the error
                        }
                    }

                }
                else if (c.Action == ChangeAction.Delete)
                {
                    logger.Info("Deleting {0}", Path.Combine(BaseDirectory.ToString(), c.FilePath));
                    if (File.Exists(Path.Combine(BaseDirectory.ToString(), c.FilePath)))
                    {
                        File.Delete(Path.Combine(BaseDirectory.ToString(), c.FilePath));
                        Console.WriteLine(Path.Combine(BaseDirectory.ToString(), c.FilePath));
                    }
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
                    // Determine all deletions first
                    foreach (HashType ht in ModHashes[indexInLocalHash].Hashes)
                    {
                        int index = NewHashes[indexInNewHash].Hashes.FindIndex(x => x.FileName == ht.FileName);
                        if (index == -1)
                        {
                            // need to add a delete change
                            ChangeList.Add(new Change(mfh.ModName.ModName + ht.FileName, ChangeAction.Delete));

                        }
                    }
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
        public bool Validate(List<ModFolderHash> RemoteHashes)
        {
            foreach (ModFolderHash mfh in RemoteHashes)
            {
                int IndexInLocal = ModHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                foreach (HashType h in mfh.Hashes)
                {
                    if (ModHashes[IndexInLocal].Hashes.Exists(x => x.FileName == h.FileName))
                    {
                        HashType remoteHash = ModHashes[IndexInLocal].Hashes.Find(x => x.FileName == h.FileName);
                        if (remoteHash.Hash == h.Hash)
                        {
                            break;
                        }
                        else
                        {
                            logger.Info("Validation of mods failed");
                            return false;
                        }
                    }
                    else
                    {
                        logger.Info("Validation of mods failed");
                        return false;
                    }
                }
            }
            logger.Info("Validation of mods passed");
            return true;

        }
        public Uri GetServerFileUri()
        {
            // TODO: Some sort of selection?
            return new Uri(SyncUris[0], "server.json");
        }
        public List<ModFolderHash> GetLocalHashes()
        {
            return ModHashes;
        }
        public DirectoryInfo GetLocalPath()
        {
            return new DirectoryInfo(LocalPath);
        }
        public void UpdateHashes()
        {
            ModHashes = HashAllMods();
        }
    }
}
