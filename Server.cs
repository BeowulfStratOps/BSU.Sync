using System;
using System.Collections.Generic;
using System.Linq;
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
        internal readonly ModFolder ModName;
        internal readonly List<HashType> Hashes;
        internal ModFolderHash(ModFolder modName, List<HashType> hashes)
        {
            ModName = modName;
            Hashes = hashes;
        }
    }

    public class Server
    {

        public delegate void ProgressUpdateEventHandler(object sender, ProgressUpdateEventArguments e);
        public delegate void FetchProgressUpdateEventHandler(object sender, ProgressUpdateEventArguments e);

        public event ProgressUpdateEventHandler ProgressUpdateEvent;
        public event FetchProgressUpdateEventHandler FetchProgessUpdateEvent;

        protected virtual void OnProgressUpdateEvent(ProgressUpdateEventArguments e)
        {
            ProgressUpdateEvent?.Invoke(this, e);
        }

        protected virtual void OnFetchProgressUpdateEvent(ProgressUpdateEventArguments e)
        {
            FetchProgessUpdateEvent?.Invoke(this, e);
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        string _localPath;
        string _serverName;
        string _serverAddress;
        int _serverPort;
        string _password;
        List<ModFolder> _mods;
        DateTime _creationDate;
        DateTime _lastUpdate;
        List<ModFolderHash> _modHashes;
        Guid _serverGuid;
        List<Uri> _syncUris;

        public bool LoadFromWeb(Uri remoteServerFile, DirectoryInfo localPath)
        {
            _logger.Info("Loading server from {0}, local path {1}", remoteServerFile, localPath);
            _localPath = localPath.ToString();

            OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 0 });

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(remoteServerFile);
                //var response = (HttpWebResponse)request.GetResponse();
                ServerFile sf = FileReader.ReadServerFileFromStream(request.GetResponse().GetResponseStream());
                if (sf == null)
                {
                    return false;
                }
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 5 });
                LoadServer(sf, _localPath);
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 });
                _modHashes = HashAllMods;
            }
            catch (WebException we)
            {
                _logger.Error(we, "Failed to load server json file");
                return false;
            }

            return true;
        }
        public void CreateNewServer(string serverName, string serverAddress, string password, int serverPort, string lPath, string outputPath, List<Uri> syncUris)
        {
            _logger.Info("Creating new server: ServerName {0}, ServerAddress {1}, Password {2}, ServerPort {3}, LPath {4}, OutputPath {5}, SyncUri[0] {6}", serverName, serverAddress, password, serverPort, lPath, outputPath, syncUris[0]);
            _serverAddress = serverAddress;
            _serverName = serverName;
            _serverPort = serverPort;
            _password = password;
            _syncUris = syncUris;
            _creationDate = DateTime.Now;
            _lastUpdate = DateTime.Now;
            _serverGuid = Guid.NewGuid();
            _localPath = outputPath;
            _syncUris = syncUris;
            UpdateServer(new DirectoryInfo(lPath));

        }
        // ReSharper disable once UnusedMember.Local
        public List<ModFolder> GetFolders()
        {
            return GetFolders(new DirectoryInfo(_localPath));
        }
        public List<ModFolder> GetFolders(DirectoryInfo filePath)
        {
            _logger.Info("Finding folders");
            var returnList = new List<ModFolder>();
            foreach (string d in Directory.GetDirectories(filePath.FullName))
            {
                _logger.Info("Found folder {0}", d.Replace(filePath.FullName, string.Empty).Replace(@"\", string.Empty));
                returnList.Add(new ModFolder(d.Replace(filePath.FullName, string.Empty).Replace(@"\", string.Empty)));
            }
            return returnList;
        }
        public List<ModFolderHash> HashAllMods
        {
            get
            {
                _logger.Info("Hashing all mods");
                //var taskList = new List<Task>();
                var returnHashes = new List<ModFolderHash>(_mods.Count);

                int currentModNumber = 1;
                int perc = 90;
                if (_mods.Count > 0)
                {
                    perc = (int)((90d / _mods.Count) * currentModNumber);
                }
                foreach (ModFolder mod in _mods)
                {
                    _logger.Info("Hashing {0}", mod.ModName);
                    List<HashType> hashes = Hash.HashFolder(_localPath + @"\" + mod.ModName);
                    returnHashes.Add(new ModFolderHash(mod, hashes));
                    _logger.Info("Hashed {0}", mod.ModName);
                    OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 + perc });
                    currentModNumber++;
                    perc = (int)((90d / _mods.Count) * currentModNumber);
                }
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 100 });
                return returnHashes;
            }
        }

        public ServerFile GetServerFile()
        {
            return new ServerFile(_serverName, _serverAddress, _serverPort, _password, _mods, _lastUpdate, _creationDate, _serverGuid, _syncUris);
        }
        public void LoadServer(ServerFile sf, string localPath)
        {
            _localPath = localPath;
            _serverName = sf.ServerName;
            _serverAddress = sf.ServerAddress;
            _serverPort = sf.ServerPort;
            _password = sf.Password;
            _mods = sf.ModFolders;
            _lastUpdate = sf.LastUpdateDate;
            _creationDate = sf.CreationDate;
            _serverGuid = sf.ServerGuid;
            _syncUris = sf.SyncUris;
        }
        public void UpdateServer(DirectoryInfo inputDirectory)
        {
            _lastUpdate = DateTime.Now;
            _mods = GetFolders(inputDirectory);
            FileWriter.WriteServerConfig(GetServerFile(), new FileInfo(Path.Combine(inputDirectory.FullName, "server.json")));
            FileCopy.CopyAll(inputDirectory, new DirectoryInfo(_localPath));
            FileCopy.CleanUpFolder(inputDirectory, new DirectoryInfo(_localPath), new DirectoryInfo(_localPath));
            // TODO: Maybe remove all zsync files?
            _modHashes = HashAllMods;
            foreach (string f in Directory.EnumerateFiles(_localPath, "*", SearchOption.AllDirectories).Where(name => !name.EndsWith(".zsync")))
            {
                ZsyncManager.Make(f);
            }
            FileWriter.WriteModHashes(_modHashes, new DirectoryInfo(_localPath));

        }
        /// <summary>
        /// Returns a list of all the mods this server is aware of
        /// </summary>
        /// <returns></returns>
        public List<ModFolder> GetLoadedMods()
        {
            return _mods;
        }
        /// <summary>
        /// Fetches changes
        /// </summary>
        /// <param name="baseDirectory">Directory to download mods to</param>
        /// <param name="newHashes">NewHashes to process</param>
        /// <returns>Number of changes that failed, 0 if none</returns>
        /// <remarks>If return > 0 then the process should be re-run</remarks>
        public int FetchChanges(DirectoryInfo baseDirectory, List<ModFolderHash> newHashes)
        {
            OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 });

            var failedChanges = new List<Change>();
            
            List<Change> changes = GenerateChangeList(newHashes);
            var tasks = new List<Task>();

            // Allocated 80% for this task (10%-90%)
            var completedTasks = 0;
            var perc = 90;
            var success = true;
            if (changes.Count > 0)
            {
                perc = (int)((80d / changes.Count) * completedTasks);
            }
            OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 + perc });
            OnFetchProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = completedTasks, MaximumValue = changes.Count });
            foreach (Change c in changes)
            {
                switch (c.Action)
                {
                    case ChangeAction.Acquire:
                        //Changes.Remove(c);
                        if (c.FilePath != "server.json")
                        {
                            //Console.WriteLine("Getting {0}",c.FilePath);
                            var reqUri = new Uri(_syncUris[0], c.FilePath + ".zsync");
                            try
                            {
                                success = true;
                                //ZsyncManager.ZsyncDownload(reqUri, BaseDirectory.ToString(), c.FilePath);
                                /*
                            if (tasks.Count > 5)
                            { 
                                Task.WaitAll(tasks.ToArray());
                            }
                            */
                                while (tasks.Count > 5)
                                {
                                    Thread.Sleep(100);
                                }
                                Task t = Task.Factory.StartNew(() => {
                                    //Console.WriteLine("Starting");
                                    try
                                    {
                                        ZsyncManager.ZsyncDownload(reqUri, baseDirectory.ToString(), c.FilePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        success = false;
                                        failedChanges.Add(c);
                                        _logger.Error(ex, "Failed to acquire file {0}", c.FilePath);
                                    }
                                });
                                t.ContinueWith((prevTask) => {
                                    //Console.WriteLine("Ending");
                                    tasks.Remove(t);
                                    if (!success) return;
                                    completedTasks++;
                                    perc = (int) ((80d/changes.Count)*completedTasks);
                                    OnProgressUpdateEvent(new ProgressUpdateEventArguments() {ProgressValue = 10 + perc});
                                    OnFetchProgressUpdateEvent(new ProgressUpdateEventArguments()
                                    {
                                        ProgressValue = completedTasks,
                                        MaximumValue = changes.Count
                                    });
                                });
                                tasks.Add(t);

                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex);
                            }
                        }
                        break;
                    case ChangeAction.Delete:
                        _logger.Info("Deleting {0}", Path.Combine(baseDirectory.ToString(), c.FilePath));
                        if (File.Exists(Path.Combine(baseDirectory.ToString(), c.FilePath)))
                        {
                            File.Delete(Path.Combine(baseDirectory.ToString(), c.FilePath));
                            Console.WriteLine(Path.Combine(baseDirectory.ToString(), c.FilePath));
                        }
                        //Changes.Remove(c);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            Task.WaitAll(tasks.ToArray());
            if (failedChanges.Count() != 0)
            {
                // Failed to acquire at least one file
                _logger.Error("Failed to complete {0}/{1} changes", failedChanges.Count(), changes.Count());
                
            }
            return failedChanges.Count();


        }
        public List<Change> GenerateChangeList(List<ModFolderHash> newHashes)
        {
            var changeList = new List<Change>();
            foreach (ModFolderHash mfh in newHashes)
            {
                if (!_modHashes.Exists(x => x.ModName.ModName == mfh.ModName.ModName))
                {
                    // If the entire mod doesn't exist, add it all
                    foreach (HashType h in mfh.Hashes)
                    {
                        changeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire));
                    }
                }
                else
                {
                    
                    int indexInLocalHash = _modHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                    int indexInNewHash = newHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                    // Determine all deletions first
                    foreach (HashType ht in _modHashes[indexInLocalHash].Hashes)
                    {
                        int index = newHashes[indexInNewHash].Hashes.FindIndex(x => x.FileName == ht.FileName);
                        if (index == -1)
                        {
                            // need to add a delete change
                            changeList.Add(new Change(mfh.ModName.ModName + ht.FileName, ChangeAction.Delete));

                        }
                    }
                    foreach (HashType h in mfh.Hashes)
                    {
                        
                        if (_modHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName))
                        {
                            // File exists both in the local hash and the remote hash
                            if (!_modHashes[indexInLocalHash]
                                .Hashes.Exists(x => x.FileName == h.FileName &&
                                                    !x.Hash.SequenceEqual(h.Hash))) continue;
                            {
                                // A file exists but has a different hash, it must be (re)acquired 
                                //HashType hash = _modHashes[indexInLocalHash].Hashes.Find(x => x.FileName == h.FileName);
                                changeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire));
                            }
                        }
                        else if (!_modHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName) && newHashes[indexInNewHash].Hashes.Exists(x => x.FileName == h.FileName ))
                        {
                            // Does not exist locally, but does exist remotely. Acquire it
                            changeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire));
                        }
                        else if (_modHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName) && !newHashes[indexInNewHash].Hashes.Exists(x => x.FileName == h.FileName))
                        {
                            // Exists locally, but does not exist remotely. Delete it
                            changeList.Add(new Change(mfh.ModName.ModName +  h.FileName, ChangeAction.Delete));
                        }
                    }
                }
            }
            return changeList;
        }
        public bool Validate(List<ModFolderHash> remoteHashes)
        {
            foreach (ModFolderHash mfh in remoteHashes)
            {
                int indexInLocal = _modHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                foreach (HashType h in mfh.Hashes)
                {
                    if (_modHashes[indexInLocal].Hashes.Exists(x => x.FileName == h.FileName))
                    {
                        HashType remoteHash = _modHashes[indexInLocal].Hashes.Find(x => x.FileName == h.FileName);
                        if (remoteHash.Hash == h.Hash)
                        {
                            break;
                        }
                        else
                        {
                            _logger.Info("Validation of mods failed");
                            return false;
                        }
                    }
                    else
                    {
                        _logger.Info("Validation of mods failed");
                        return false;
                    }
                }
            }
            _logger.Info("Validation of mods passed");
            return true;

        }
        public Uri GetServerFileUri()
        {
            // TODO: Some sort of selection?
            return new Uri(_syncUris[0], "server.json");
        }
        public List<ModFolderHash> GetLocalHashes()
        {
            return _modHashes;
        }
        public DirectoryInfo GetLocalPath()
        {
            return new DirectoryInfo(_localPath);
        }
        public void UpdateHashes()
        {
            _modHashes = HashAllMods;
        }
    }
}
